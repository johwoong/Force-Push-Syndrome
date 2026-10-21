using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 장착 장비의 등급 효과 합산 결과
// 퍼센트(HP·공격·이동)는 보너스%로, 나머지는 더하기 값으로 쓴다
public struct EquipmentEffectSummary
{
    // Stat형
    public int HpPercent;           // 최대 체력 보너스%
    public int AttackPercent;       // 공격력 보너스%
    public int Defense;             // 장비 방어력 고정값
    public int MoveSpeedPercent;    // 이동속도 % (장비 moveSpeedBonus 합에 더함)
    public int CriticalChance;      // 치명타 확률 %p
    public int CriticalDamage;      // 치명타 피해 %p
    public int DodgeChance;         // 회피 확률 %p
    public int DamageReduction;     // 받는 피해 감소 %p
    public int HealReceived;        // 받는 치유량 %p

    // Trigger형
    public float NegateDamageChance;   // 피격 시 피해 무효 확률 % (여러 개면 합산)
    public float IntervalHealPercent;  // 주기 회복: 최대 체력의 % (여러 개면 합산)
    public float IntervalHealSeconds;  // 주기 회복 간격 (0이면 주기 회복 없음)

    public int AppliedEffectCount;     // 적용된 효과 수 (Stat + Trigger)

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("체력 +").Append(HpPercent).Append("%")
          .Append(" / 공격 +").Append(AttackPercent).Append("%")
          .Append(" / 방어 +").Append(Defense)
          .Append(" / 이동 +").Append(MoveSpeedPercent).Append("%")
          .Append(" / 치명 확률 +").Append(CriticalChance).Append("%p")
          .Append(" / 치명 피해 +").Append(CriticalDamage).Append("%p")
          .Append(" / 회피 +").Append(DodgeChance).Append("%p")
          .Append(" / 받는 피해 -").Append(DamageReduction).Append("%p")
          .Append(" / 치유량 +").Append(HealReceived).Append("%p")
          .Append(" / 피해 무효 ").Append(NegateDamageChance).Append("%")
          .Append(" / 주기 회복 ").Append(IntervalHealPercent).Append("% (")
          .Append(IntervalHealSeconds).Append("초)")
          .Append(" / 적용 효과 ").Append(AppliedEffectCount).Append("개");
        return sb.ToString();
    }
}

// 장착 장비의 활성 등급 효과를 모아 합산한다
// - 장비 인스턴스 등급 이하(General → 인스턴스 등급)에 연결된 효과를 모두 적용 (누적)
// - 제외(79xxx)는 건너뜀
// - Stat형은 statType별 합산, Trigger형은 피해 무효 확률·주기 회복 % 합산
// - Buff(72xxx)·Conditional(73xxx)은 계산하지 않음 (아이템 상세에 문구만 표시)
// - 장비 레벨·등급 배율은 등급 효과에 적용하지 않음
public static class EquipmentEffectResolver
{
    private static readonly ItemGrade[] GradeOrder = { ItemGrade.General, ItemGrade.Super, ItemGrade.Rare };

    public static EquipmentEffectSummary Resolve(IEnumerable<OwnedItem> equipped)
    {
        var summary = new EquipmentEffectSummary();

        var json = GameManager.JsonData;
        if (json == null || equipped == null)
            return summary;

        foreach (var item in equipped)
        {
            if (item == null) continue;

            foreach (var grade in GradeOrder)
            {
                if (grade > item.Grade) break;   // 인스턴스 등급보다 높은 등급은 잠김

                foreach (var effect in json.GetItemGradeEffectsFromJson(item.itemId, grade))
                {
                    if (effect == null || effect.IsExcluded) continue;

                    switch (effect.Category)
                    {
                        case EquipEffectCategory.Stat:
                            AddStat(ref summary, effect);
                            summary.AppliedEffectCount++;
                            break;

                        case EquipEffectCategory.Trigger:
                            if (AddTrigger(ref summary, effect))
                                summary.AppliedEffectCount++;
                            break;

                            // Buff, Conditional: 1차 범위 밖 (표시만)
                    }
                }
            }
        }

        return summary;
    }

    private static void AddStat(ref EquipmentEffectSummary summary, EquipEffectData effect)
    {
        int value = ToInt(effect);

        switch (effect.StatType)
        {
            case EquipEffectStatType.HP: summary.HpPercent += value; break;
            case EquipEffectStatType.ATTACK: summary.AttackPercent += value; break;
            case EquipEffectStatType.DEFENSE: summary.Defense += value; break;
            case EquipEffectStatType.MOVE_SPEED: summary.MoveSpeedPercent += value; break;
            case EquipEffectStatType.CRITICAL_CHANCE: summary.CriticalChance += value; break;
            case EquipEffectStatType.CRITICAL_DAMAGE: summary.CriticalDamage += value; break;
            case EquipEffectStatType.DODGE_CHANCE: summary.DodgeChance += value; break;
            case EquipEffectStatType.DAMAGE_REDUCTION: summary.DamageReduction += value; break;
            case EquipEffectStatType.HEAL_RECEIVED: summary.HealReceived += value; break;
            default:
                Debug.LogWarning("[EquipmentEffectResolver] 처리하지 않는 statType: " + effect.effectId + " " + effect.statType);
                break;
        }
    }

    // 지원하는 조합이면 true
    private static bool AddTrigger(ref EquipmentEffectSummary summary, EquipEffectData effect)
    {
        if (effect.TriggerType == EquipEffectTriggerType.OnDamaged && effect.ActionType == EquipEffectActionType.NegateDamage)
        {
            summary.NegateDamageChance += effect.triggerChance;
            return true;
        }

        if (effect.TriggerType == EquipEffectTriggerType.Interval && effect.ActionType == EquipEffectActionType.HealMaxHpPercent)
        {
            summary.IntervalHealPercent += effect.actionValue;

            // 타이머는 하나 (지금 데이터는 모두 3초). 간격이 다르면 처음 값을 유지하고 알림
            if (summary.IntervalHealSeconds <= 0f)
                summary.IntervalHealSeconds = effect.triggerInterval;
            else if (!Mathf.Approximately(summary.IntervalHealSeconds, effect.triggerInterval))
                Debug.LogWarning("[EquipmentEffectResolver] 주기 회복 간격이 서로 다릅니다: " + summary.IntervalHealSeconds + "초 / " +
                                 effect.triggerInterval + "초 (" + effect.effectId + "). 처음 값으로 계산합니다.");
            return true;
        }

        Debug.LogWarning("[EquipmentEffectResolver] 처리하지 않는 Trigger 조합: " + effect.effectId + " " +
                         effect.triggerType + " / " + effect.actionType);
        return false;
    }

    // 지금 데이터 기준 정수로 계산. 소수가 들어오면 버리고 알림 (소수 지원은 나중에)
    private static int ToInt(EquipEffectData effect)
    {
        int value = (int)effect.effectValue;
        if (!Mathf.Approximately(effect.effectValue, value))
            Debug.LogWarning("[EquipmentEffectResolver] 소수 효과 값은 아직 지원하지 않아 정수로 버립니다: " +
                             effect.effectId + " " + effect.statType + " " + effect.effectValue);
        return value;
    }
}