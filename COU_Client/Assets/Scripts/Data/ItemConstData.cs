using System;

// 장비 강화·합성 상수. ItemConst.json의 유일한 행
[Serializable]
public class ItemConstData
{
    public int maxLevel;
    public int levelUpBaseCost;
    public float statGrowthPerLevel;
    public int synthesisMaterialCount;
    public float[] gradeStatMultiplier;   // ItemGrade 순서
}
