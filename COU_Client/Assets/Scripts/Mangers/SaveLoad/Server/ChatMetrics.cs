using System;
using System.Collections.Generic;
using UnityEngine;

// 채팅 전송 방식 측정용. 폴링/WebSocket 모두 같은 지표로 비교하기 위한 클래스
public class ChatMetrics
{
    private string transport;
    private float startedAt;
    private int requestCount;      // 서버로 보낸 요청(REST 조회) 수
    private int dataResponses;     // 새 메시지가 들어 있던 응답 수
    private int pushCount;         // WebSocket 으로 받은 푸시 수
    private readonly List<double> latencies = new List<double>();

    public void Begin(string transportName)
    {
        transport = transportName;
        startedAt = Time.realtimeSinceStartup;
        requestCount = 0;
        dataResponses = 0;
        pushCount = 0;
        latencies.Clear();
    }

    public void OnRequest() => requestCount++;

    // WebSocket 으로 새 메시지를 받았을 때 호출
    public void OnPush() => pushCount++;

    // 새 메시지가 도착했을 때 호출 (내가 보낸 메시지는 제외)
    public void OnReceived(List<ChatMessageResponse> list)
    {
        var now = DateTime.UtcNow;
        bool any = false;

        foreach (var m in list)
        {
            if (m.mine) continue;
            latencies.Add((now - m.createdAt.ToUniversalTime()).TotalMilliseconds);
            any = true;
        }

        if (any) dataResponses++;
    }

    public string Summary()
    {
        float sec = Time.realtimeSinceStartup - startedAt;
        float perMin = sec > 0 ? requestCount / sec * 60f : 0;
        int empty = requestCount - dataResponses;
        float emptyRate = requestCount > 0 ? (float)Math.Max(0, empty) / requestCount * 100f : 0;

        string latency = "수신 메시지 없음";
        if (latencies.Count > 0)
        {
            var sorted = new List<double>(latencies);
            sorted.Sort();
            double avg = 0;
            foreach (var v in sorted) avg += v;
            avg /= sorted.Count;
            double p95 = sorted[Math.Max(0, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];

            latency = $"수신 {sorted.Count}건 / 평균 {avg:F0}ms / p95 {p95:F0}ms / 최대 {sorted[sorted.Count - 1]:F0}ms";
        }

        return $"[ChatMetrics:{transport}] {sec:F0}초 / 요청 {requestCount}회 (분당 {perMin:F1}회) / 빈 응답 {emptyRate:F0}% / 푸시 {pushCount}건 / {latency}";
    }
}
