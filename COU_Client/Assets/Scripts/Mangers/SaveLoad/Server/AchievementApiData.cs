using System;

// 서버 업적 API 응답 형식 (필드 이름은 서버 응답과 같게 유지)

// GET /api/players/me/achievements 의 항목 하나
[Serializable]
public class PlayerAchievementData
{
    public int achievementId;
    public long progress;       // 누적 진행 값
    public int completedStep;   // 달성한 단계 수 (서버 계산)
    public int claimedStep;     // 보상 받은 단계 수

    public bool CanClaim => completedStep > claimedStep;
}

// POST /api/players/me/achievements/{achievementId}/claim
[Serializable]
public class AchievementClaimResponse
{
    public CurrencyData currency;
    public PlayerAchievementData achievement;
}
