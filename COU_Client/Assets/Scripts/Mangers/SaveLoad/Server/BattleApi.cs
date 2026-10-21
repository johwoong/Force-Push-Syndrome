using System;
using System.Collections;

// 전투 입장·결과 서버 API 호출부. 응답 반영과 요청 중 상태는 호출하는 쪽(BattleTab, BattleManager)이 맡는다
// 사용 : StartCoroutine(BattleApi.Enter(stageId, result => { .... }));
public static class BattleApi
{
    public const string EnterPath = "/api/battles";
    public const string ResultPathFormat = "/api/battles/{0}/result";   // {0}: battleId
    public const string PatrolPath = "/api/battles/patrol";

    // 서버 에러 code
    public const string ErrorStageLocked = "STAGE_LOCKED";
    public const string ErrorNotEnoughStamina = "NOT_ENOUGH_STAMINA";
    public const string ErrorBattleAlreadyCompleted = "BATTLE_ALREADY_COMPLETED";
    public const string ErrorBattleExpired = "BATTLE_EXPIRED";
    public const string ErrorNoClearedStage = "NO_CLEARED_STAGE";

    // 전투 입장: 서버가 해금과 스태미나를 검증해 차감하고 battleId를 발급한다
    public static IEnumerator Enter(int stageId, Action<ApiResult<BattleEnterResponse>> onComplete)
        => ApiClient.Post(EnterPath, new BattleEnterRequest { stageId = stageId }, onComplete);

    // 전투 결과: 서버가 상한으로 검증해 지급한다
    public static IEnumerator SendResult(long battleId, BattleResultRequest body, Action<ApiResult<BattleResultResponse>> onComplete)
        => ApiClient.Post(string.Format(ResultPathFormat, battleId), body, onComplete);

    // 순찰: 서버가 스태미나를 차감하고 최고 클리어 스테이지의 평균 보상을 지급한다
    public static IEnumerator Patrol(Action<ApiResult<PatrolResponse>> onComplete)
        => ApiClient.Post(PatrolPath, null, onComplete);
}
