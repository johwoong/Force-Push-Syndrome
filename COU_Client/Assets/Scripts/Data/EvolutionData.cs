using System;
using System.Collections.Generic;

[Serializable]
public class EvolutionData
{
    public string[] evolutionStatOrder;
    public int[] evolutionIncreasePerLevel;
    public int evolutionBaseCost;
    public int evolutionCostPerStep;
    public int evolutionMaxStep;
    public int evolutionStepsPerLevel;

    private static readonly string[] RequiredStatTypes =
    {
        PlayerStatType.Attack, PlayerStatType.Hp, PlayerStatType.Defense, PlayerStatType.PotionRecovery
    };

    public void Validate()
    {
        if (evolutionStatOrder == null || evolutionIncreasePerLevel == null)
            throw new InvalidOperationException("Evolution의 evolutionStatOrder, evolutionIncreasePerLevel이 비어있습니다.");

        if (evolutionStatOrder.Length != evolutionIncreasePerLevel.Length)
            throw new InvalidOperationException("Evolution의 두 배열 길이가 다릅니다.");

        var seen = new HashSet<string>();
        foreach (string statType in evolutionStatOrder)
        {
            if (Array.IndexOf(RequiredStatTypes, statType) < 0)
                throw new InvalidOperationException("Evolution에 알 수 없는 스탯이 있습니다: " + statType);
            if (!seen.Add(statType))
                throw new InvalidOperationException("Evolution에 중복된 스탯이 있습니다: " + statType);
        }

        if (seen.Count != RequiredStatTypes.Length)
            throw new InvalidOperationException("Evolution에 4종 스탯이 모두 있어야 합니다.");

        foreach (int increase in evolutionIncreasePerLevel)
        {
            if (increase < 0)
                throw new InvalidOperationException("Evolution의 증가량은 0 이상이어야 합니다.");
        }

        if (evolutionBaseCost < 0 || evolutionCostPerStep < 0 || evolutionMaxStep < 0)
            throw new InvalidOperationException("Evolution의 비용·최대 단계는 0 이상이어야 합니다.");

        if (evolutionStepsPerLevel < 1)
            throw new InvalidOperationException("Evolution의 evolutionStepsPerLevel은 1 이상이어야 합니다.");
    }
    
}
