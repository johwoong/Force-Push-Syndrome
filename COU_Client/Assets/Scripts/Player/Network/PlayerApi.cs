using System;
using System.Collections;

// 플레이어 스탯 서버 API 호출부
// 최종 스탯(E2)은 클라이언트 전투에는 쓰지 않고, 랭킹·검증용으로 유지
// 사용: StartCoroutine(PlayerApi.GetFinalStats(result => { ... }));
public static class PlayerApi
{
    public const string FinalStatsPath = "/api/players/me/stats";

    // 최종 스탯 조회
    public static IEnumerator GetFinalStats(Action<ApiResult<FinalStatsResponse>> onComplete)
        => ApiClient.Get(FinalStatsPath, onComplete);
}