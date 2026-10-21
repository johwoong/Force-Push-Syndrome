
using System;

// 효과 분류
public enum EquipEffectCategory
{
    None,
    Stat,
    Trigger,
    Buff,
    Conditional
}

// 효과가 변경하는 스탯
public enum EquipEffectStatType
{
    None,
    HP,
    ATTACK,
    DEFENSE,
    MOVE_SPEED,
    CRITICAL_CHANCE,
    CRITICAL_DAMAGE,
    DODGE_CHANCE,
    DAMAGE_REDUCTION,
    HEAL_RECEIVED
}

// 스탯 수치 적용 방식
public enum EquipEffectValueType
{
    None,
    Percent,
    Add
}

// 발동 조건
public enum EquipEffectTriggerType
{
    None,
    OnDamaged,
    Interval
}

// 발동 시 실행할 행동
public enum EquipEffectActionType
{
    None,
    NegateDamage,
    HealMaxHpPercent
}

[Serializable]
public class EquipEffectData
{
    // JSON 원본 데이터
    public long effectId;
    public string effectDescription;
    public string effectCategory;
    public string statType;
    public string valueType;
    public float effectValue;
    public string triggerType;
    public float triggerChance;
    public float triggerInterval;
    public string actionType;
    public float actionValue;

    // JSON 로딩 후 변환된 enum
    [NonSerialized]
    private EquipEffectCategory parsedCategory;

    [NonSerialized]
    private EquipEffectStatType parsedStatType;

    [NonSerialized]
    private EquipEffectValueType parsedValueType;

    [NonSerialized]
    private EquipEffectTriggerType parsedTriggerType;

    [NonSerialized]
    private EquipEffectActionType parsedActionType;

    public EquipEffectCategory Category => parsedCategory;
    public EquipEffectStatType StatType => parsedStatType;
    public EquipEffectValueType ValueType => parsedValueType;
    public EquipEffectTriggerType TriggerType => parsedTriggerType;
    public EquipEffectActionType ActionType => parsedActionType;

    // effectId의 두 번째 자리가 9이면 제외 대상
    public bool IsExcluded => (effectId / 1000) % 10 == 9;

    // JsonDataManager가 역직렬화 직후 호출
    public void OnLoaded()
    {
        if (!Enum.TryParse(effectCategory, out parsedCategory)
            || !Enum.IsDefined(typeof(EquipEffectCategory), parsedCategory))
        {
            throw new InvalidOperationException(
                $"EquipEffect {effectId}: 잘못된 effectCategory '{effectCategory}'");
        }

        if (!Enum.TryParse(statType, out parsedStatType)
            || !Enum.IsDefined(typeof(EquipEffectStatType), parsedStatType))
        {
            throw new InvalidOperationException(
                $"EquipEffect {effectId}: 잘못된 statType '{statType}'");
        }

        if (!Enum.TryParse(valueType, out parsedValueType)
            || !Enum.IsDefined(typeof(EquipEffectValueType), parsedValueType))
        {
            throw new InvalidOperationException(
                $"EquipEffect {effectId}: 잘못된 valueType '{valueType}'");
        }

        if (!Enum.TryParse(triggerType, out parsedTriggerType)
            || !Enum.IsDefined(typeof(EquipEffectTriggerType), parsedTriggerType))
        {
            throw new InvalidOperationException(
                $"EquipEffect {effectId}: 잘못된 triggerType '{triggerType}'");
        }

        if (!Enum.TryParse(actionType, out parsedActionType)
            || !Enum.IsDefined(typeof(EquipEffectActionType), parsedActionType))
        {
            throw new InvalidOperationException(
                $"EquipEffect {effectId}: 잘못된 actionType '{actionType}'");
        }
    }
}
