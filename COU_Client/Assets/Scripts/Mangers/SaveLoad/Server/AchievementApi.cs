using System;
using System.Collections;
using System.Collections.Generic;

// 업적 서버 API 호출부
public static class AchievementApi 
{
    public const string AchievementsPath = "/api/players/me/achievements";
    
    // 서버 에러 code
    public const string ErrorNothingToClaim = "NOTHING_TO_CLAIM";
    
    public static IEnumerator GetAchievements(Action<ApiResult<List<PlayerAchievementData>>> onComplete)
        => ApiClient.Get(AchievementsPath, onComplete);

    public static IEnumerator Claim(int achievementId, Action<ApiResult<AchievementClaimResponse>> onComplete)
        => ApiClient.Post($"{AchievementsPath}/{achievementId}/claim", null, onComplete);
}
