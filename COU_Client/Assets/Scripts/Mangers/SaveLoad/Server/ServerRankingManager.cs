using System;
using UnityEngine;

public class ServerRankingManager : MonoSingleton<ServerRankingManager>
{
    // onComplete(성공 여부, 실패 메시지, 응답)
    public void Fetch(RankingType type, int? stageId, int limit, Action<bool, string, RankingResponse> onComplete)
    {
        StartCoroutine(RankingApi.Get(type, stageId, limit, result =>
        {
            if (!result.IsSuccess)
            {
                onComplete?.Invoke(false, result.ToUserMessage("랭킹을 불러오지 못했습니다."), null);
                return;
            }
            onComplete?.Invoke(true, null, result.Data);
        }));
    }
}
