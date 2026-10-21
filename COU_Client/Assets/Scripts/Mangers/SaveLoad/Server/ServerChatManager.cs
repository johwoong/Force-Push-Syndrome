using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// 1:1 채팅. 새 메시지는 WebSocket 으로 서버가 밀어주고, 보내기/불러오기는 REST 를 그대로 쓴다.
// - 대화 시작: REST 로 최근 메시지를 불러온 뒤 WebSocket 연결
// - 연결 직후/재연결 직후: afterId 로 놓친 메시지를 한 번 복구 조회
// - WebSocket 이 끊겨 있는 동안(3초 이상)만 예전처럼 폴링해서 채팅이 끊기지 않게 한다 (대체 경로)
public class ServerChatManager : MonoSingleton<ServerChatManager>
{
    private const float FallbackPollInterval = 2f;   // 대체 폴링 주기
    private const float FallbackStartDelay = 3f;     // 끊긴 뒤 이 시간이 지나야 대체 폴링 시작
    private const int PageSize = 50;
    private const string SocketPath = "/ws/chat";

    // 새 메시지가 도착했을 때 (내가 보낸 것 포함)
    public event Action<List<ChatMessageResponse>> MessagesReceived;

    public long CurrentFriendId { get; private set; }
    public bool InConversation => CurrentFriendId != 0;
    public bool IsSocketConnected => socket != null && socket.IsConnected;

    // 측정용: 서버로 보낸 조회 요청 횟수 / 전송 방식별 지표
    public int PollRequestCount { get; private set; }
    public ChatMetrics Metrics { get; } = new ChatMetrics();

    private int session;                // 대화가 바뀔 때마다 증가. 지난 대화의 늦은 응답을 버리는 용도
    private long lastMessageId;         // 마지막으로 받은 메시지 ID (놓친 메시지 복구용)
    private readonly HashSet<long> seenIds = new();   // 이미 화면에 전달한 메시지 (중복 방지)
    private bool isRequesting;
    private Coroutine fallbackRoutine;

    private ChatSocket socket;
    private CancellationTokenSource socketCts;
    private float disconnectedSince;

    // 대화 시작: 최근 메시지를 불러오고, 성공하면 WebSocket 을 연결한다
    public void StartConversation(long friendPlayerId, Action<bool, string, List<ChatMessageResponse>> onComplete)
    {
        StopConversation();
        CurrentFriendId = friendPlayerId;
        int mySession = session;

        StartCoroutine(ChatApi.GetMessages(friendPlayerId, 0, PageSize, r =>
        {
            if (mySession != session) return;   // 그 사이 대화가 닫히거나 바뀜

            if (!r.IsSuccess)
            {
                onComplete?.Invoke(false, r.ToUserMessage("대화를 불러오지 못했습니다."), null);
                return;
            }

            var list = r.Data ?? new List<ChatMessageResponse>();
            MarkSeen(list);
            onComplete?.Invoke(true, null, list);
            BeginRealtime(mySession, friendPlayerId);
        }));
    }

    // 대화 종료
    public void StopConversation()
    {
        if (socket != null) Debug.Log(Metrics.Summary());   // 측정이 시작된 대화만 출력

        session++;
        CurrentFriendId = 0;
        lastMessageId = 0;
        seenIds.Clear();
        isRequesting = false;

        if (fallbackRoutine != null)
        {
            StopCoroutine(fallbackRoutine);
            fallbackRoutine = null;
        }

        StopSocket();
    }

    // 메시지 보내기 (REST). 응답으로 받은 내 메시지는 바로 화면에 전달한다
    public void Send(string content, Action<bool, string> onComplete)
    {
        if (!InConversation)
        {
            onComplete?.Invoke(false, "대화 상대가 없습니다.");
            return;
        }

        int mySession = session;
        long friendId = CurrentFriendId;

        StartCoroutine(ChatApi.Send(friendId, content, r =>
        {
            if (!r.IsSuccess)
            {
                onComplete?.Invoke(false, r.ToUserMessage("메시지를 보내지 못했습니다."));
                return;
            }

            onComplete?.Invoke(true, null);

            if (mySession == session && r.Data != null)
                Deliver(new List<ChatMessageResponse> { r.Data });
        }));
    }

    // ===== WebSocket =====

    private void BeginRealtime(int mySession, long friendId)
    {
        Metrics.Begin("WebSocket");
        disconnectedSince = Time.realtimeSinceStartup;

        socket = new ChatSocket(BuildSocketUrl(), () => ApiClient.AccessToken);
        socketCts = new CancellationTokenSource();

        var runningSocket = socket;
        var token = socketCts.Token;
        _ = Task.Run(() => runningSocket.RunAsync(token));   // 연결/수신은 백그라운드에서

        fallbackRoutine = StartCoroutine(FallbackPollLoop(mySession, friendId));
    }

    private void StopSocket()
    {
        if (socketCts != null)
        {
            socketCts.Cancel();
            socketCts = null;
        }
        socket = null;
    }

    private static string BuildSocketUrl()
    {
        string baseUrl = ApiClient.BaseUrl.TrimEnd('/');

        if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            baseUrl = "wss://" + baseUrl.Substring("https://".Length);
        else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            baseUrl = "ws://" + baseUrl.Substring("http://".Length);

        return baseUrl + SocketPath;
    }

    // 백그라운드에서 쌓아둔 소켓 이벤트를 메인 스레드에서 처리한다
    private void Update()
    {
        var current = socket;
        if (current == null) return;

        while (current.TryDequeue(out var socketEvent))
        {
            if (!InConversation) continue;

            switch (socketEvent.Type)
            {
                case ChatSocketEventType.Connected:
                    Debug.Log("[ServerChat] WebSocket 연결됨");
                    // 첫 조회와 연결 사이, 또는 끊겨 있던 사이에 온 메시지를 한 번 가져온다
                    StartCoroutine(RecoverOnce(session, CurrentFriendId));
                    break;

                case ChatSocketEventType.Closed:
                    Debug.LogWarning("[ServerChat] WebSocket 끊김. 자동으로 다시 연결합니다.");
                    disconnectedSince = Time.realtimeSinceStartup;
                    break;

                case ChatSocketEventType.Message:
                    OnPushed(socketEvent.Push);
                    break;
            }
        }
    }

    private void OnPushed(ChatPushEvent push)
    {
        // 지금 열어둔 대화 상대가 보낸 메시지만 처리한다
        if (push.friendId != CurrentFriendId) return;

        Metrics.OnPush();
        Deliver(new List<ChatMessageResponse> { push.message });
    }

    // ===== 복구 / 대체 폴링 (REST) =====

    // WebSocket 이 끊겨 있는 동안에만 폴링한다
    private IEnumerator FallbackPollLoop(int mySession, long friendId)
    {
        var wait = new WaitForSeconds(FallbackPollInterval);

        while (mySession == session)
        {
            yield return wait;
            if (mySession != session) yield break;

            if (IsSocketConnected) continue;
            if (Time.realtimeSinceStartup - disconnectedSince < FallbackStartDelay) continue;

            yield return RecoverOnce(mySession, friendId);
        }
    }

    // lastMessageId 이후의 메시지를 REST 로 가져온다
    private IEnumerator RecoverOnce(int mySession, long friendId)
    {
        while (isRequesting) yield return null;   // 진행 중인 요청이 끝날 때까지 기다린다
        if (mySession != session) yield break;

        isRequesting = true;
        PollRequestCount++;
        Metrics.OnRequest();

        yield return ChatApi.GetMessages(friendId, lastMessageId, PageSize, r =>
        {
            if (mySession != session) return;   // 지난 대화의 응답이면 버린다
            isRequesting = false;

            if (!r.IsSuccess || r.Data == null || r.Data.Count == 0) return;

            Deliver(r.Data);

            // 한 번에 다 못 가져왔으면 이어서 가져온다
            if (r.Data.Count >= PageSize)
                StartCoroutine(RecoverOnce(mySession, friendId));
        });
    }

    // ===== 전달 =====

    // 아직 전달하지 않은 메시지만 골라 화면(MessagesReceived)으로 보낸다.
    // WebSocket 푸시, REST 복구, 내가 보낸 메시지 응답이 겹쳐도 한 번만 보이게 한다.
    private void Deliver(List<ChatMessageResponse> list)
    {
        List<ChatMessageResponse> fresh = null;

        foreach (var m in list)
        {
            if (!seenIds.Add(m.messageId)) continue;

            (fresh ??= new List<ChatMessageResponse>()).Add(m);
            if (m.messageId > lastMessageId) lastMessageId = m.messageId;
        }

        if (fresh == null) return;

        Metrics.OnReceived(fresh);
        MessagesReceived?.Invoke(fresh);
    }

    private void MarkSeen(List<ChatMessageResponse> list)
    {
        foreach (var m in list)
        {
            seenIds.Add(m.messageId);
            if (m.messageId > lastMessageId) lastMessageId = m.messageId;
        }
    }

    private void OnApplicationQuit()
    {
        StopSocket();
    }

    protected override void OnDestroy()
    {
        StopSocket();
        base.OnDestroy();
    }
}
