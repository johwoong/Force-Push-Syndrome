using System;
using UnityEngine;


[Serializable]
public class OwnedItem
{
    public string instanceId;
    public long itemId;
    public bool isEquipped;
    public int level = 1;

    public ItemGrade grade;

    public ItemData Data => GameManager.JsonData.GetItemDataFromJson(itemId);
    public ItemGrade Grade => grade;

    public int MaxLevel => ItemLevelConfig.MaxLevel;
    public bool CanLevelUp => level < MaxLevel;
    public int NextLevelUpCost => ItemLevelConfig.GetLevelUpCost(level);

    public bool CanSynthesize => grade != ItemGrade.Rare;
    public ItemGrade NextGrade => grade + 1;

    private decimal StatMultiplier => ItemLevelConfig.GetStatMultiplierExact(level) * ItemLevelConfig.GetGradeMultiplierExact(grade);

    private int Scale(int baseValue)
    => (int)Math.Round(baseValue * StatMultiplier, MidpointRounding.ToEven);

    public int ScaledHp => Scale(Data.hpBonus);
    public int ScaledAttack => Scale(Data.attackBonus);
    public int ScaledMoveSpeed => Scale(Data.moveSpeedBonus);

    public OwnedItem(long itemId)
    {
        instanceId = Guid.NewGuid().ToString();
        this.itemId = itemId;
        grade = Data.Grade;
    }

}
