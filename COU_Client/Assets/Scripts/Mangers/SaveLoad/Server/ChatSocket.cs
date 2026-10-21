using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

// 서버가 WebSocket 으로 밀어주는 채팅 이벤트 (서버 ChatPushEvent 와 같은 모양)
[Serializable]
public class ChatPushEvent
{
    public string type;                 // "connected" | "message" | "pong"
    public long friendId;               // 메시지를 보낸 상대
    public ChatMessageResponse message; // 받는 사람 기준(mine=false)
}

public enum ChatSocketEventType
{
    Connected,  // 연결 성공
    Closed,     // 연결이 끊김 (자동으로 다시 연결을 시도한다)
    Message     // 새 채팅 메시지
}

public class ChatSocketEvent
{
    public ChatSocketEventType Type;
    public ChatPushEvent Push;
}

// 서버(/ws/chat)와의 WebSocket 연결 하나를 관리한다.
// - 백그라운드(스레드풀)에서 연결/수신하고, 결과는 큐에 쌓는다. Unity 쪽은 메인 스레드에서 TryDequeue 로 꺼내 쓴다.
// - 끊기면 1 -> 2 -> 4 -> 8초 간격으로 다시 연결한다.
// - Unity API 는 Debug.Log 외에는 쓰지 않는다 (스레드 안전).
public class ChatSocket
{
    private const int ReceiveBufferSize = 4096;
    private const int PingIntervalMs = 25_000;   // 연결 유지용 ping 간격
    private const int MaxBackoffSeconds = 8;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly string url;
    private readonly Func<string> accessTokenProvider;
    private readonly ConcurrentQueue<ChatSocketEvent> events = new();
    private volatile bool isConnected;

    public bool IsConnected => isConnected;

    public ChatSocket(string url, Func<string> accessTokenProvider)
    {
        this.url = url;
        this.accessTokenProvider = accessTokenProvider;
    }

    public bool TryDequeue(out ChatSocketEvent socketEvent) => events.TryDequeue(out socketEvent);

    // 취소될 때까지 연결 -> 수신 -> (끊기면) 재연결을 반복한다.
    public async Task RunAsync(CancellationToken ct)
    {
        int failures = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                bool wasConnected = await RunOnceAsync(ct);
                isConnected = false;
                if (ct.IsCancellationRequested) break;

                events.Enqueue(new ChatSocketEvent { Type = ChatSocketEventType.Closed });

                failures = wasConnected ? 0 : failures + 1;
                int delaySeconds = Math.Min(MaxBackoffSeconds, 1 << Math.Min(failures, 3));  // 1, 2, 4, 8
                await Task.Delay(delaySeconds * 1000, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // 대화를 닫아서 취소됨
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ChatSocket] 예기치 못한 종료: " + e);
        }
        finally
        {
            isConnected = false;
        }
    }

    // 연결을 한 번 맺고 끊길 때까지 수신한다. 연결에 성공한 적이 있으면 true.
    private async Task<bool> RunOnceAsync(CancellationToken ct)
    {
        string token = accessTokenProvider();
        if (string.IsNullOrEmpty(token)) return false;

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Authorization", "Bearer " + token);

        try
        {
            await ws.ConnectAsync(new Uri(url), ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ChatSocket] 연결 실패: " + e.Message);
            return false;
        }

        isConnected = true;
        events.Enqueue(new ChatSocketEvent { Type = ChatSocketEventType.Connected });

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = PingLoopAsync(ws, linked.Token);

        try
        {
            await ReceiveLoopAsync(ws, linked.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ChatSocket] 연결 끊김: " + e.Message);
        }
        finally
        {
            linked.Cancel();
            await CloseQuietlyAsync(ws);
        }

        return true;
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[ReceiveBufferSize];
        using var message = new MemoryStream();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            message.SetLength(0);
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close) return;
                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);   // 한 메시지가 여러 조각으로 올 수 있다

            if (result.MessageType != WebSocketMessageType.Text) continue;

            HandleText(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
        }
    }

    private void HandleText(string json)
    {
        ChatPushEvent push;
        try
        {
            push = JsonConvert.DeserializeObject<ChatPushEvent>(json, JsonSettings);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ChatSocket] 메시지 변환 실패: " + e.Message + "\n" + json);
            return;
        }

        // connected / pong 은 알릴 필요가 없다. 새 메시지만 전달한다.
        if (push == null || push.type != "message" || push.message == null) return;

        events.Enqueue(new ChatSocketEvent { Type = ChatSocketEventType.Message, Push = push });
    }

    // 일정 간격으로 ping 을 보내 중간 장비(공유기, 로드밸런서)가 연결을 끊지 않게 한다.
    private async Task PingLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var ping = new ArraySegment<byte>(Encoding.UTF8.GetBytes("ping"));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(PingIntervalMs, ct);
                if (ws.State != WebSocketState.Open) break;
                await ws.SendAsync(ping, WebSocketMessageType.Text, true, ct);
            }
        }
        catch
        {
            // 연결이 끝났거나 취소됨
        }
    }

    private static async Task CloseQuietlyAsync(ClientWebSocket ws)
    {
        try
        {
            using var timeout = new CancellationTokenSource(1000);

            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", timeout.Token);
            else if (ws.State == WebSocketState.CloseReceived)
                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", timeout.Token);
        }
        catch
        {
            // 이미 끊긴 연결
        }
    }
}
