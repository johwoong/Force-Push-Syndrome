using System;
using System.Collections;
using UnityEngine;

// 받은 친구 요청 수. 친구 버튼의 빨간 배지와 친구 팝업이 같이 쓴다
public static class FriendRequestState
{
    public static int Count { get; private set; }

    public static event Action<int> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Count = 0;
        Changed = null;
    }

    public static void Set(int count)
    {
        if (Count == count) return;

        Count = count;
        Changed?.Invoke(Count);
    }

    // 서버에서 받은 요청 수를 다시 받아 온다
    public static void Refresh()
    {
        if (!ApiClient.HasToken) return;

        GameManager.ServerFriend.FetchRequests((success, message, requests) =>
        {
            if (success) Set(requests?.Count ?? 0);
        });
    }
}

// 친구 버튼 위 빨간 배지: 받은 친구 요청이 있을 때만 보인다
public class FriendBadge : MonoBehaviour
{
    private const float PollSeconds = 30f;   // 팝업을 열지 않아도 새 요청이 오면 알 수 있게 주기적으로 확인

    [SerializeField] private RectTransform badge;

    private void OnEnable()
    {
        FriendRequestState.Changed += Apply;
        Apply(FriendRequestState.Count);

        StartCoroutine(PollLoop());
    }

    private void OnDisable()
    {
        FriendRequestState.Changed -= Apply;
    }

    private IEnumerator PollLoop()
    {
        var wait = new WaitForSeconds(PollSeconds);
        while (true)
        {
            FriendRequestState.Refresh();
            yield return wait;
        }
    }

    private void Apply(int count)
    {
        var show = count > 0;
        var wasShown = badge.gameObject.activeSelf;

        badge.gameObject.SetActive(show);
        if (show && !wasShown)
            UITween.PopIn(badge, 0f, 0f);
    }
}
