using System;
using System.Collections;
using UnityEngine;

public static class RankingApi
{
    public const string Path = "/api/ranking";

    public static IEnumerator Get(RankingType type, int? stageId, int limit,
        Action<ApiResult<RankingResponse>> onComplete)
    {
        string query = $"?type={type}&limit={limit}";
        if (stageId.HasValue) query += $"&stageId={stageId.Value}";
        return ApiClient.Get(Path + query, onComplete);
    }
}
