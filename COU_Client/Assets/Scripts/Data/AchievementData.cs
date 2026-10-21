using System;

// 시트 값과 이름이 같아야 한다 (Enum.TryParse는 대소문자를 구분한다.)
public enum AchievementConditionType
{
    KillBySkill, PickupDropItem, UpgradeSkill, GainBattleExp,
    GainBattleGold, ClearStage, PlayLuckTrain
}

public enum AchievementRewardType { Gold, Gem }

// 업적 하나. 단계는 배열 순서이고, targetValue·rewardItemType·rewardAmount의 같은 칸이 한 단계다.
// Achievement.json의 조건·드롭 아이템·보상 문자열을 enum으로 바꿔서 쓴다.
public class AchievementData
{
    public int achievementId;
    public string achievementName;
    public string achievementDescription;   // {0}에 그 단계의 목표값을 넣어 표시
    public string achievementConditionType;
    public long targetSkillId;               // 0이면 대상 없음
    public string targetDropItemType;       // DropItemType 이름 또는 "None"
    public int[] targetValue;
    public string[] rewardItemType;
    public int[] rewardAmount;
    
    [NonSerialized] private AchievementConditionType parsedConditionType;
    public AchievementConditionType ConditionType => parsedConditionType;

    [NonSerialized] private DropItemType parsedDropItemType;
    public DropItemType TargetDropItemType => parsedDropItemType;
    public bool HasTargetDropItem => targetDropItemType != "None";

    [NonSerialized] private AchievementRewardType[] parsedRewardTypes;

    public int TierCount => targetValue.Length;
    public int GetTargetValue(int tier) => targetValue[tier];
    public AchievementRewardType GetRewardType(int tier) => parsedRewardTypes[tier];
    public int GetRewardAmount(int tier) => rewardAmount[tier];
    public string GetDescription(int tier) => string.Format(achievementDescription, targetValue[tier]);
    
    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출. 형식이 맞지 안으면 예외를 던진다.
    public void OnLoaded()
    {
        string name = $"Achievement {achievementId}";
        
        if (!Enum.TryParse(achievementConditionType, out parsedConditionType))
            throw new InvalidOperationException($"{name}의 achievementConditionType \"{achievementConditionType}\"을(를) 알 수 없습니다.");

        if (HasTargetDropItem && !Enum.TryParse(targetDropItemType, out parsedDropItemType))
            throw new InvalidOperationException($"{name}의 targetDropItemType \"{targetDropItemType}\"을(를) 알 수 없습니다.");

        if (targetValue == null || targetValue.Length == 0)
            throw new InvalidOperationException($"{name}의 targetValue가 비어 있습니다.");

        if (rewardItemType == null || rewardAmount == null
                                   || rewardItemType.Length != targetValue.Length || rewardAmount.Length != targetValue.Length)
            throw new InvalidOperationException($"{name}: targetValue, rewardItemType, rewardAmount의 개수가 같아야 합니다.");

        parsedRewardTypes = new AchievementRewardType[rewardItemType.Length];
        for (int i = 0; i < targetValue.Length; i++)
        {
            if (targetValue[i] <= 0 || (i > 0 && targetValue[i] <= targetValue[i - 1]))
                throw new InvalidOperationException($"{name}의 targetValue는 0보다 크고 단계마다 커져야 합니다.");

            if (!Enum.TryParse(rewardItemType[i], out parsedRewardTypes[i]))
                throw new InvalidOperationException($"{name}의 rewardItemType \"{rewardItemType[i]}\"을(를) 알 수 없습니다.");
        }
    }
}
