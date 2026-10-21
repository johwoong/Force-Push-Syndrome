using System;
using System.Collections;

// 진화 처리: 서버 요청 + 응답의 진화 레벨·재화를 계정 데이터에 덮어쓰기
// 골드는 직접 빼지 않고 서버 응답 값으로 덮어쓴다 (전투 입장 응답 처리와 같은 방식)
// 사용: StartCoroutine(EvolutionService.Upgrade(result => { ... }));
public static class EvolutionService
{
    public static IEnumerator Upgrade(Action<ApiResult<EvolutionUpgradeResponse>> onComplete)
    {
        yield return EvolutionApi.Upgrade(result =>
        {
            try
            {
                if (result.IsSuccess && result.Data != null)
                    Apply(result.Data);
            }
            catch (Exception e)
            {
                // 화면 갱신 중 에러가 나도 요청 완료 처리(onComplete)는 반드시 실행
                UnityEngine.Debug.LogException(e);
            }

            onComplete?.Invoke(result);
        });
    }

    private static void Apply(EvolutionUpgradeResponse response)
    {
        var dataManager = GameManager.PlayerData;
        if (dataManager == null || !dataManager.isPlayerDataLoaded) return;

        var data = dataManager.currentData;

        if (response.playerStat != null)
            data.playerStat = response.playerStat;

        if (response.currency != null)
            data.currency = response.currency;

        dataManager.NotifyPlayerDataChanged();
    }
}