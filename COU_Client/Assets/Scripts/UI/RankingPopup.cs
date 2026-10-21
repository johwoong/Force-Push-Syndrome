using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingPopup : UIPopup
{
    [Serializable]
    private class TabEntry
    {
        public Button button;
        public RankingType type;
    }

    private const int FetchLimit = 50;
    private const int PodiumCount = 3;

    private static readonly Color TabSelectedColor = new Color(0.2f, 0.6039216f, 0.9411765f);
    private static readonly Color TabNormalColor = new Color(0.21568628f, 0.2627451f, 0.33333334f);

    [SerializeField] private Button closeButton;
    [SerializeField] private TabEntry[] tabs;

    [Header("스테이지 선택 (생존 시간 탭)")]
    [SerializeField] private Button prevStageButton;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private TMP_Text subTitleText;

    [Header("상위 3명 (0 = 1위, 1 = 2위, 2 = 3위)")]
    [SerializeField] private RankingPodiumView[] podiums;

    [Header("4위 이하 목록")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private RankingEntryView entryPrefab;
    [SerializeField] private TMP_Text statusText;

    [Header("내 순위")]
    [SerializeField] private TMP_Text myRankText;
    [SerializeField] private TMP_Text myValueText;

    private readonly List<RankingEntryView> entryViews = new();

    private RankingType currentType = RankingType.COMBAT_POWER;
    private int[] stageIds = Array.Empty<int>();
    private int stageIndex;
    private int requestId; // 탭/스테이지를 빠르게 바꿨을 때 늦게 온 응답을 버리기 위한 번호

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        prevStageButton.onClick.AddListener(() => ChangeStage(-1));
        nextStageButton.onClick.AddListener(() => ChangeStage(1));

        foreach (var tab in tabs)
        {
            var type = tab.type;
            tab.button.onClick.AddListener(() => SelectTab(type));
        }
    }

    public override void Open()
    {
        base.Open();

        LoadStageIds();
        SelectTab(currentType);
    }

    private void LoadStageIds()
    {
        var dic = GameManager.JsonData.StageDataDic;
        stageIds = dic == null ? Array.Empty<int>() : dic.Keys.OrderBy(id => id).ToArray();
        stageIndex = Mathf.Clamp(stageIndex, 0, Mathf.Max(0, stageIds.Length - 1));
    }

    private void SelectTab(RankingType type)
    {
        currentType = type;

        foreach (var tab in tabs)
            tab.button.targetGraphic.color = tab.type == type ? TabSelectedColor : TabNormalColor;

        Refresh();
    }

    private void ChangeStage(int delta)
    {
        stageIndex = Mathf.Clamp(stageIndex + delta, 0, Mathf.Max(0, stageIds.Length - 1));
        Refresh();
    }

    private void Refresh()
    {
        bool isSurvival = currentType == RankingType.STAGE_SURVIVAL;

        prevStageButton.gameObject.SetActive(isSurvival);
        nextStageButton.gameObject.SetActive(isSurvival);
        prevStageButton.interactable = stageIndex > 0;
        nextStageButton.interactable = stageIndex < stageIds.Length - 1;

        int? stageId = null;
        if (isSurvival)
        {
            if (stageIds.Length == 0)
            {
                subTitleText.text = "스테이지 정보가 없습니다";
                ClearView();
                ShowStatus("스테이지 정보가 없습니다.");
                return;
            }
            stageId = stageIds[stageIndex];
        }

        subTitleText.text = GetSubTitle(currentType, stageId);

        int id = ++requestId;
        ClearView();
        ShowStatus("불러오는 중...");

        GameManager.ServerRanking.Fetch(currentType, stageId, FetchLimit, (success, message, response) =>
        {
            if (id != requestId) return; // 이미 다른 탭/스테이지로 넘어감

            if (!success)
            {
                ShowStatus(message);
                return;
            }

            Render(response);
        });
    }

    private void Render(RankingResponse response)
    {
        var entries = response.entries ?? new List<RankingEntryResponse>();

        // 1~3위 포디움
        for (int i = 0; i < PodiumCount; i++)
        {
            if (i < entries.Count)
                podiums[i].Setup(entries[i], FormatValue(currentType, entries[i].value), response.myRank == entries[i].rank, OnAddFriend);
            else
                podiums[i].Clear();
        }

        // 4위 이하 목록
        bool showLevel = currentType != RankingType.ACCOUNT_LEVEL;
        for (int i = PodiumCount; i < entries.Count; i++)
        {
            var entry = entries[i];
            var view = Instantiate(entryPrefab, listRoot);
            view.Setup(entry, FormatValue(currentType, entry.value), showLevel, response.myRank == entry.rank, OnAddFriend);
            entryViews.Add(view);
        }

        if (entries.Count == 0)
            ShowStatus("아직 랭킹 기록이 없습니다.");
        else
            statusText.gameObject.SetActive(false);

        SetMine(response);

        // 포디움은 3위 → 2위 → 1위 순으로, 목록은 위에서부터 차례로 나온다
        UITween.Stagger(podiums.Reverse(), 0.12f);
        UITween.Stagger(entryViews, 0.03f);
    }

    private bool isSendingFriendRequest;

    private void OnAddFriend(RankingEntryResponse entry)
    {
        if (isSendingFriendRequest) return;   // 연타 방지
        isSendingFriendRequest = true;

        GameManager.ServerFriend.SendRequest(entry.playerId, (success, message, response) =>
        {
            isSendingFriendRequest = false;

            var popup = UIManager.Instance.Get<MessagePopup>();
            if (!success)
            {
                popup.ShowAlert(message);   // 서버 에러 메시지: 이미 친구, 요청 보냄, 한도 초과 등
                return;
            }

            bool nowFriend = response != null && response.relation == FriendRelation.FRIEND;
            popup.ShowAlert(nowFriend
                ? $"'{entry.nickname}'님과 친구가 되었습니다!"
                : $"'{entry.nickname}'님에게 친구 요청을 보냈습니다.");
        });
    }

    private void SetMine(RankingResponse response)
    {
        if (response == null || response.myRank == null || response.myValue == null)
        {
            myRankText.text = "내 순위  -";
            myValueText.text = "기록 없음";
            return;
        }

        myRankText.text = $"내 순위  {response.myRank}위";
        myValueText.text = FormatValue(currentType, response.myValue.Value);
    }

    private void ClearView()
    {
        foreach (var podium in podiums)
            podium.Clear();

        foreach (var view in entryViews)
            Destroy(view.gameObject);
        entryViews.Clear();

        myRankText.text = "내 순위  -";
        myValueText.text = string.Empty;
    }

    private void ShowStatus(string message)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }

    private static string GetSubTitle(RankingType type, int? stageId)
    {
        switch (type)
        {
            case RankingType.ACCOUNT_LEVEL: return "계정 레벨이 높은 순서";
            case RankingType.MAX_STAGE: return "최고 클리어 스테이지 순서";
            case RankingType.STAGE_SURVIVAL: return $"스테이지 {stageId}  ·  생존 시간 순";
            case RankingType.COMBAT_POWER: return "전투력이 높은 순서";
            default: return string.Empty;
        }
    }

    private static string FormatValue(RankingType type, int value)
    {
        switch (type)
        {
            case RankingType.ACCOUNT_LEVEL: return $"Lv.{value}";
            case RankingType.MAX_STAGE: return $"스테이지 {value}";
            case RankingType.STAGE_SURVIVAL: return $"{value / 60}:{value % 60:00}"; // 초 -> m:ss
            case RankingType.COMBAT_POWER: return value.ToString("N0");
            default: return value.ToString();
        }
    }
}
