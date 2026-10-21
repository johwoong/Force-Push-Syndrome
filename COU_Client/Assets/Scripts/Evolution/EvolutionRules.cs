using UnityEngine;

// 진화 규칙 (서버와 같은 공식)
// - 진화 단계 = 4개 진화 레벨의 합
// - 다음 스탯 = evolutionStatOrder[단계 % 배열 길이]
// - 다음 비용 = evolutionBaseCost + evolutionCostPerStep × 단계
// - 최대 단계 = evolutionMaxStep > 0 이고 단계 ≥ evolutionMaxStep
// - 진화 스탯 = 스탯별 레벨 × 그 스탯의 evolutionIncreasePerLevel
public static class EvolutionRules
{
    // 진화를 막는 이유 (서버와 같은 판단 순서: 최대 단계 → 계정 레벨 → 골드)
    public enum EvolutionBlock
    {
        None,
        MaxStepReached,   // 진화 최대 단계 도달 (레벨을 올려도 더 진화 불가)
        LevelLocked,      // 계정 레벨 제한
        NotEnoughGold
    }

    // 지금 진화할 수 있는지, 없다면 이유 (서버 E1 검사 순서와 동일: 최대 단계 → 계정 레벨 → 골드)
    public static EvolutionBlock GetBlock(EvolutionData data, int step, int accountLevel, int gold)
    {
        if (data == null) return EvolutionBlock.None;

        if (data.evolutionMaxStep > 0 && step >= data.evolutionMaxStep)
            return EvolutionBlock.MaxStepReached;

        if (step >= System.Math.Max(accountLevel, 0) * data.evolutionStepsPerLevel)
            return EvolutionBlock.LevelLocked;

        if (GetNextCost(data, step) > gold)
            return EvolutionBlock.NotEnoughGold;

        return EvolutionBlock.None;
    }

    public static EvolutionData Data => GameManager.JsonData != null ? GameManager.JsonData.EvolutionData : null;
    // 계정 레벨로 도달할 수 있는 최대 진화 단계 (evolutionMaxStep이 있으면 둘 중 작은 값)
    public static int GetMaxStepByAccountLevel(EvolutionData data, int accountLevel)
    {
        if (data == null) return 0;

        int max = System.Math.Max(accountLevel, 0) * data.evolutionStepsPerLevel;
        if (data.evolutionMaxStep > 0)
            max = System.Math.Min(max, data.evolutionMaxStep);
        return max;
    }

    // s번째 진화(0부터)에 필요한 계정 레벨 (레벨당 4단계면 0~3 → 1, 4~7 → 2)
    public static int GetRequiredAccountLevel(EvolutionData data, int step)
    {
        if (data == null || data.evolutionStepsPerLevel < 1) return 1;
        return step / data.evolutionStepsPerLevel + 1;
    }
    

    public static int GetStep(PlayerStatData stat)
    {
        if (stat == null) return 0;
        return stat.playerStatAttackLevel + stat.playerStatHpLevel + stat.playerStatDefenseLevel + stat.playerStatPotionRecoveryLevel;
    }

    public static int GetLevel(PlayerStatData stat, string statType)
    {
        if (stat == null) return 0;

        return statType switch
        {
            PlayerStatType.Attack => stat.playerStatAttackLevel,
            PlayerStatType.Hp => stat.playerStatHpLevel,
            PlayerStatType.Defense => stat.playerStatDefenseLevel,
            PlayerStatType.PotionRecovery => stat.playerStatPotionRecoveryLevel,
            _ => 0
        };
    }

    public static int GetIncreasePerLevel(EvolutionData data, string statType)
    {
        if (data == null) return 0;

        int index = System.Array.IndexOf(data.evolutionStatOrder, statType);
        return index < 0 ? 0 : data.evolutionIncreasePerLevel[index];
    }

    public static int GetBonus(EvolutionData data, PlayerStatData stat, string statType)
        => GetLevel(stat, statType) * GetIncreasePerLevel(data, statType);

    public static string GetNextStatType(EvolutionData data, int step)
    {
        if (data == null || data.evolutionStatOrder.Length == 0) return null;
        return data.evolutionStatOrder[step % data.evolutionStatOrder.Length];
    }

    public static int GetNextCost(EvolutionData data, int step)
        => data == null ? 0 : data.evolutionBaseCost + data.evolutionCostPerStep * step;
}
