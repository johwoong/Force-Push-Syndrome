using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendPopup : UIPopup
{
    private enum Tab { Friends = 0, Requests = 1, Add = 2 }

    private const int MaxReceivedRequests = 50;
    private const int MaxSearchLength = 20;
    private const float AutoRefreshSeconds = 15f;  

    private static readonly Color TabSelectedColor = new Color(0.2f, 0.6039216f, 0.9411765f);
    private static readonly Color TabNormalColor = new Color(0.21568628f, 0.2627451f, 0.33333334f);
    private static readonly Color OnlineColor = new Color(0.55f, 0.9f, 0.62f);
    private static readonly Color NeutralColor = new Color(0.62f, 0.67f, 0.74f);

    [SerializeField] private Button closeButton;
    [SerializeField] private Button addShortcutButton;

    [Header("탭 (0 = 친구 목록, 1 = 친구 요청, 2 = 친구 추가)")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private TMP_Text requestTabLabel;

    [Header("검색")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;

    [Header("목록")]
    [SerializeField] private GameObject countRow;
    [SerializeField] private Image countDot;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private FriendRowView rowPrefab;
    [SerializeField] private TMP_Text statusText;

    private readonly List<FriendRowView> rows = new();

    private List<FriendResponse> friends = new();
    private List<FriendRequestResponse> requests = new();
    private List<FriendSearchResponse> searchResults = new();
    private int maxFriends = 50;
    private bool searched;      

    private Tab currentTab = Tab.Friends;
    private int requestSerial;     
    private bool isBusy;         
    private Coroutine autoRefresh;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        addShortcutButton.onClick.AddListener(() => SelectTab(Tab.Add));

        for (int i = 0; i < tabButtons.Length; i++)
        {
            var tab = (Tab)i;
            tabButtons[i].onClick.AddListener(() => SelectTab(tab));
        }

        searchInput.characterLimit = MaxSearchLength;
        searchInput.onValueChanged.AddListener(OnSearchTextChanged);
        searchInput.onSubmit.AddListener(_ => { if (currentTab == Tab.Add) Search(); });
        searchButton.onClick.AddListener(Search);
    }

    public override void Open()
    {
        base.Open();

        GameManager.ServerPresence.StartHeartbeat();

        searched = false;
        searchResults.Clear();

        SelectTab(Tab.Friends);
        FetchRequestCount();

        if (autoRefresh != null) StopCoroutine(autoRefresh);
        autoRefresh = StartCoroutine(AutoRefreshLoop());
    }

    public override void Close()
    {
        if (autoRefresh != null)
        {
            StopCoroutine(autoRefresh);
            autoRefresh = null;
        }

        base.Close();
    }

    private IEnumerator AutoRefreshLoop()
    {
        var wait = new WaitForSeconds(AutoRefreshSeconds);
        while (true)
        {
            yield return wait;

            if (currentTab == Tab.Friends) LoadFriends(false);
            FetchRequestCount();
        }
    }


    private void SelectTab(Tab tab)
    {
        currentTab = tab;

        for (int i = 0; i < tabButtons.Length; i++)
            tabButtons[i].targetGraphic.color = i == (int)tab ? TabSelectedColor : TabNormalColor;

        searchInput.SetTextWithoutNotify(string.Empty);
        searchButton.gameObject.SetActive(tab == Tab.Add);
        ((TMP_Text)searchInput.placeholder).text = tab switch
        {
            Tab.Friends => "친구 닉네임 검색",
            Tab.Requests => "요청 닉네임 검색",
            _ => "닉네임 또는 플레이어 ID 검색"
        };

        switch (tab)
        {
            case Tab.Friends: LoadFriends(true); break;
            case Tab.Requests: LoadRequests(); break;
            case Tab.Add: RenderSearch(); break;
        }
    }

    private void OnSearchTextChanged(string _)
    {
       
        if (currentTab == Tab.Friends) RenderFriends();
        else if (currentTab == Tab.Requests) RenderRequests();
    }

    // ===== 친구 목록 =====

    private void LoadFriends(bool showLoading)
    {
        int id = ++requestSerial;

        if (showLoading)
        {
            ClearRows();
            countRow.SetActive(false);
            ShowStatus("불러오는 중...");
        }

        GameManager.ServerFriend.FetchFriends((success, message, response) =>
        {
            if (id != requestSerial || currentTab != Tab.Friends) return;

            if (!success)
            {
                if (showLoading) ShowStatus(message);
                return;
            }

            friends = response.friends ?? new List<FriendResponse>();
            if (response.maxFriends > 0) maxFriends = response.maxFriends;
            RenderFriends();
            if (showLoading) PopRows();   // 주기적 갱신 때는 목록이 튀지 않게 그대로 둔다
        });
    }

    private void RenderFriends()
    {
        ClearRows();

        int onlineCount = friends.Count(f => f.online);
        countRow.SetActive(true);
        countDot.gameObject.SetActive(true);
        countDot.color = OnlineColor;
        countText.color = OnlineColor;
        countText.text = $"온라인 친구 {onlineCount}명  ({friends.Count}/{maxFriends})";

        string filter = searchInput.text.Trim();
        var shown = friends.Where(f => Matches(f.nickname, f.playerId, filter)).ToList();

        foreach (var friend in shown)
        {
            var f = friend;
            var row = CreateRow();
            row.SetInfo(f.nickname, $"{(f.online ? "온라인" : "오프라인")}  ·  전투력 {f.combatPower:N0}", f.online);
            row.SetOnClick(() => OnChat(f));          // 줄 전체를 누르면 채팅
            row.SetDelete(() => ConfirmRemove(f));    // 빨간 쓰레기통 버튼
        }

        if (friends.Count == 0) ShowStatus("아직 친구가 없습니다.\n'친구 추가' 탭에서 친구를 찾아보세요.");
        else if (shown.Count == 0) ShowStatus("검색 결과가 없습니다.");
        else HideStatus();
    }

    private void OnChat(FriendResponse friend)
    {
        UIManager.Instance.Get<ChatPopup>().Show(friend.playerId, friend.nickname);
    }

    private void ConfirmRemove(FriendResponse friend)
    {
        UIManager.Instance.Get<MessagePopup>().ShowConfirm($"'{friend.nickname}'님을 친구에서 삭제할까요?", () =>
        {
            if (isBusy) return;
            isBusy = true;

            GameManager.ServerFriend.Remove(friend.playerId, (success, message) =>
            {
                isBusy = false;

                if (!success)
                {
                    Alert(message);
                    return;
                }

                friends.RemoveAll(f => f.playerId == friend.playerId);
                if (currentTab == Tab.Friends) RenderFriends();
            });
        });
    }


    private void LoadRequests()
    {
        int id = ++requestSerial;

        ClearRows();
        countRow.SetActive(false);
        ShowStatus("불러오는 중...");

        GameManager.ServerFriend.FetchRequests((success, message, response) =>
        {
            if (id != requestSerial || currentTab != Tab.Requests) return;

            if (!success)
            {
                ShowStatus(message);
                return;
            }

            requests = response ?? new List<FriendRequestResponse>();
            UpdateRequestBadge();
            RenderRequests();
            PopRows();
        });
    }

    private void RenderRequests()
    {
        ClearRows();

        countRow.SetActive(true);
        countDot.gameObject.SetActive(false);
        countText.color = NeutralColor;
        countText.text = $"받은 요청 {requests.Count}/{MaxReceivedRequests}";

        string filter = searchInput.text.Trim();
        var shown = requests.Where(r => Matches(r.nickname, r.playerId, filter)).ToList();

        foreach (var request in shown)
        {
            var r = request;
            var row = CreateRow();
            row.SetInfo(r.nickname, $"Lv.{r.accountLevel}", null);
            row.SetPrimary("수락", FriendRowView.BlueButton, true, () => Accept(r));
            row.SetSecondary("거절", FriendRowView.GrayButton, true, () => Reject(r));
        }

        if (requests.Count == 0) ShowStatus("받은 친구 요청이 없습니다.");
        else if (shown.Count == 0) ShowStatus("검색 결과가 없습니다.");
        else HideStatus();
    }

    private void Accept(FriendRequestResponse request)
    {
        if (isBusy) return;
        isBusy = true;

        GameManager.ServerFriend.Accept(request.requestId, (success, message, friend) =>
        {
            isBusy = false;

            if (!success)
            {
                Alert(message);
                LoadRequests();   // 이미 취소된 요청일 수 있어서 목록을 다시 받는다
                return;
            }

            RemoveRequest(request);
        });
    }

    private void Reject(FriendRequestResponse request)
    {
        if (isBusy) return;
        isBusy = true;

        GameManager.ServerFriend.Reject(request.requestId, (success, message) =>
        {
            isBusy = false;

            if (!success)
            {
                Alert(message);
                LoadRequests();
                return;
            }

            RemoveRequest(request);
        });
    }

    private void RemoveRequest(FriendRequestResponse request)
    {
        requests.RemoveAll(r => r.requestId == request.requestId);
        UpdateRequestBadge();
        if (currentTab == Tab.Requests) RenderRequests();
    }


    private void FetchRequestCount()
    {
        GameManager.ServerFriend.FetchRequests((success, message, response) =>
        {
            if (!success) return;

            requests = response ?? new List<FriendRequestResponse>();
            UpdateRequestBadge();
        });
    }

    private void UpdateRequestBadge()
    {
        requestTabLabel.text = requests.Count > 0 ? $"친구 요청 ({requests.Count})" : "친구 요청";
        FriendRequestState.Set(requests.Count);   // 친구 버튼의 빨간 배지도 같이 갱신
    }

    // ===== 친구 추가 (검색) =====

    private void Search()
    {
        if (currentTab != Tab.Add) return;

        string keyword = searchInput.text.Trim();
        if (keyword.Length == 0)
        {
            Alert("닉네임 또는 플레이어 ID를 입력해 주세요.");
            return;
        }

        int id = ++requestSerial;
        searched = true;

        ClearRows();
        countRow.SetActive(false);
        ShowStatus("검색 중...");

        GameManager.ServerFriend.Search(keyword, (success, message, response) =>
        {
            if (id != requestSerial || currentTab != Tab.Add) return;

            if (!success)
            {
                ShowStatus(message);
                return;
            }

            searchResults = response ?? new List<FriendSearchResponse>();
            RenderSearch();
            PopRows();
        });
    }

    private void RenderSearch()
    {
        ClearRows();

        if (!searched)
        {
            countRow.SetActive(false);
            ShowStatus("닉네임 또는 플레이어 ID로\n친구를 찾아보세요.");
            return;
        }

        countRow.SetActive(true);
        countDot.gameObject.SetActive(false);
        countText.color = NeutralColor;
        countText.text = $"검색 결과 {searchResults.Count}명";

        foreach (var result in searchResults)
        {
            var s = result;
            var row = CreateRow();
            row.SetInfo(s.nickname, $"Lv.{s.accountLevel}  ·  ID {s.playerId}", null);
            row.HideSecondary();

            switch (s.relation)
            {
                case FriendRelation.FRIEND:
                    row.SetPrimary("친구", FriendRowView.DisabledButton, false, null);
                    break;
                case FriendRelation.REQUEST_SENT:
                    row.SetPrimary("요청함", FriendRowView.DisabledButton, false, null);
                    break;
                case FriendRelation.REQUEST_RECEIVED:
                    // 상대가 먼저 보낸 요청이 있으면 서버가 바로 친구로 만들어 준다
                    row.SetPrimary("수락", FriendRowView.BlueButton, true, () => SendRequest(s));
                    break;
                default:
                    row.SetPrimary("친구 신청", FriendRowView.BlueButton, true, () => SendRequest(s));
                    break;
            }
        }

        if (searchResults.Count == 0) ShowStatus("검색 결과가 없습니다.");
        else HideStatus();
    }

    private void SendRequest(FriendSearchResponse target)
    {
        if (isBusy) return;
        isBusy = true;

        GameManager.ServerFriend.SendRequest(target.playerId, (success, message, response) =>
        {
            isBusy = false;

            if (!success)
            {
                Alert(message);
                return;
            }

            bool nowFriend = response != null && response.relation == FriendRelation.FRIEND;
            target.relation = nowFriend ? FriendRelation.FRIEND : FriendRelation.REQUEST_SENT;

            if (currentTab == Tab.Add) RenderSearch();
            if (nowFriend) Alert($"'{target.nickname}'님과 친구가 되었습니다!");
        });
    }

    // ===== 공통 =====
    private FriendRowView CreateRow()
    {
        var row = Instantiate(rowPrefab, listRoot);
        rows.Add(row);
        return row;
    }

    private void ClearRows()
    {
        foreach (var row in rows)
            Destroy(row.gameObject);
        rows.Clear();
    }

    // 서버에서 새로 받아 그린 목록만 차례로 나오게 한다
    private void PopRows()
        => UITween.Stagger(rows, 0.03f);

    private void ShowStatus(string message)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }

    private void HideStatus()
        => statusText.gameObject.SetActive(false);

    private static bool Matches(string nickname, long playerId, string filter)
        => string.IsNullOrEmpty(filter)
           || nickname.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
           || playerId.ToString() == filter;

    private static void Alert(string message)
        => UIManager.Instance.Get<MessagePopup>().ShowAlert(message);
}
