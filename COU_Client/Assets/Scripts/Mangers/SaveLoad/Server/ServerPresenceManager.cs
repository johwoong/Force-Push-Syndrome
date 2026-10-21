using System.Collections;
using UnityEngine;

public class ServerPresenceManager : MonoSingleton<ServerPresenceManager>
{
    private const float IntervalSeconds = 30f;   // 서버 TTL이 60초라 절반 주기로 보낸다
    private Coroutine loop;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this) DontDestroyOnLoad(gameObject);   // 씬이 바뀌어도 유지
    }

    public void StartHeartbeat()
    {
        if (loop != null) return;
        loop = StartCoroutine(HeartbeatLoop());
    }

    public void StopHeartbeat()
    {
        if (loop == null) return;
        StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator HeartbeatLoop()
    {
        var wait = new WaitForSeconds(IntervalSeconds);
        while (true)
        {
            if (ApiClient.HasToken)
                yield return ApiClient.Post<object>("/api/presence/heartbeat", null, null);
            yield return wait;
        }
    }
}