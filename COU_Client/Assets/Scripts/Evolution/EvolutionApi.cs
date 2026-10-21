using System;
using System.Collections;

// 진화 서버 API 호출부
// 사용: StartCoroutine(EvolutionApi.Upgrade(result => { ... }));
public static class EvolutionApi
{
    public const string UpgradePath = "/api/players/me/evolution/upgrade";

    // 서버 에러 code (진화 탭 안내 문구 분기용)
    public const string ErrorNotEnoughGold = "NOT_ENOUGH_GOLD";
    public const string ErrorMaxStepReached = "EVOLUTION_MAX_STEP_REACHED";
    public const string ErrorLevelLocked = "EVOLUTION_LEVEL_LOCKED";

    // 진화 1단계 업그레이드 (무엇을 올릴지는 서버가 결정, 요청 본문 없음)
    public static IEnumerator Upgrade(Action<ApiResult<EvolutionUpgradeResponse>> onComplete)
        => ApiClient.Post(UpgradePath, null, onComplete);
}