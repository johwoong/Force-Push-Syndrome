using System;

// 계정 공용 상수. AccountConst.json의 유일한 행
[Serializable]
public class AccountConstData
{
    public int initialGold;
    public int initialGem;
    public int initialStamina;
    public int maxStamina;
    public int accountBaseRequiredExp;
    public int accountRequiredExpIncrement;
    public int maxAccountLevel;
    public int battleStaminaCost;   // 전투 입장 스태미나 비용
    public int staminaRecoverySeconds;
    public int accountExpPerKill;
    public int accountExpPerSecond;
    public int luckTrainGoldMax;
    public int patrolStaminaCost;   // 순찰 스태미나 비용

    // level에서 다음 레벨까지 필요한 계정 경험치
    public int GetRequiredExp(int level) => accountBaseRequiredExp + accountRequiredExpIncrement * (level - 1);
}
