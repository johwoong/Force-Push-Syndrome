using UnityEngine;


public static class ItemLevelConfig
{
    private static ItemConstData Const => GameManager.JsonData.ItemConstData;

    public static int MaxLevel => Const.maxLevel;


    // 장비 합성 갯수
    public static int SynthesisMaterialCount => Const.synthesisMaterialCount;


    public static int GetLevelUpCost(int currentLevel) => Const.levelUpBaseCost * currentLevel;

    public static float GetStatMultiplier(int level) => 1f + Const.statGrowthPerLevel * (level - 1);

    public static float GetGradeMultiplier(ItemGrade grade) => Const.gradeStatMultiplier[(int)grade];

    public static decimal GetStatMultiplierExact(int level)
    => 1m + (decimal)Const.statGrowthPerLevel * (level - 1);

    public static decimal GetGradeMultiplierExact(ItemGrade grade)
        => (decimal)Const.gradeStatMultiplier[(int)grade];
}
