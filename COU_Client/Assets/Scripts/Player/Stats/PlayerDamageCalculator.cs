using System;
using UnityEngine;

// 플레이어가 주는 피해의 최종 계산 (공격력, 스킬 피해 스탯, 치명타)
// 스킬은 자기 몫인 "스킬 배율"(skillDamage × 레벨 배율 × (1 + 패시브%))만 넘기고, 맞힐 때 이 함수를 부른다
public static class PlayerDamageCalculator
{
    // true: 기본 공격력일 때 피해 = 스킬 배율 (몬스터 체력 기획 전까지)
    // false: 피해 = 공격력 × 스킬 배율 (몬스터 체력 기획 후 전환)
    public static readonly bool UseRelativeAttack = false;

    public readonly struct HitResult
    {
        public readonly int damage;
        public readonly bool isCritical;

        public HitResult(int damage, bool isCritical)
        {
            this.damage = damage;
            this.isCritical = isCritical;
        }
    }

    // 최종 피해 = 스킬 배율 × 공격력 기준값 × (스킬 피해% ÷ 100) → 치명타 → 반올림, 최소 1
    public static HitResult Calculate(float skillMultiplier, PlayerStats stats)
    {
        if (stats == null || skillMultiplier <= 0f)
            return new HitResult(0, false);

        float damage = GetBaseDamage(skillMultiplier, stats);

        bool isCritical = RollCritical(stats.CriticalChance);
        if (isCritical)
            damage *= stats.CriticalDamage / 100f;

        return new HitResult(Mathf.Max(1, Mathf.RoundToInt(damage)), isCritical);
    }

    // 치명타 판정 없이 일반·치명타 피해를 미리 계산 (로그·UI 표시용, Calculate와 같은 공식)
    public static (int normal, int critical) Preview(float skillMultiplier, PlayerStats stats)
    {
        if (stats == null || skillMultiplier <= 0f)
            return (0, 0);

        float damage = GetBaseDamage(skillMultiplier, stats);
        int normal = Mathf.Max(1, Mathf.RoundToInt(damage));
        int critical = Mathf.Max(1, Mathf.RoundToInt(damage * stats.CriticalDamage / 100f));
        return (normal, critical);
    }

    // 치명타 전 피해 (반올림 전)
    private static float GetBaseDamage(float skillMultiplier, PlayerStats stats)
        => skillMultiplier * GetAttackFactor(stats) * (stats.SkillDamage / 100f);

    private static float GetAttackFactor(PlayerStats stats)
    {
        if (!UseRelativeAttack)
            return stats.FinalAtk;

        int baseAttack = GameManager.JsonData.PlayerBaseStatData.playerBaseAttack;
        return baseAttack > 0 ? stats.FinalAtk / (float)baseAttack : 1f;
    }

    // 치명타 확률(%)로 판정. 0 이하면 없음, 100 이상이면 항상
    private static bool RollCritical(int chance)
    {
        if (chance <= 0) return false;
        if (chance >= 100) return true;
        return UnityEngine.Random.Range(0, 100) < chance;
    }

    // ===== 받는 피해 =====

    // 방어력 기준값: 방어력이 이 값이면 받는 피해가 절반
    public const int DefenseConstant = 100;

    // 피해가 들어왔을 때 최소로 받는 피해 (회피·무효로 0이 된 경우는 제외)
    public const int MinReceivedDamage = 1;

    public readonly struct ReceivedDamageResult
    {
        public readonly int damage;
        public readonly bool isDodged;
        public readonly bool isNegated;

        public ReceivedDamageResult(int damage, bool isDodged, bool isNegated)
        {
            this.damage = damage;
            this.isDodged = isDodged;
            this.isNegated = isNegated;
        }
    }

    // 받는 피해: 회피 → 피해 무효 → 방어력 → 받는 피해 감소 → 반올림(짝수 쪽) 한 번 → 최소 1
    public static ReceivedDamageResult CalculateReceived(int damage, PlayerStats stats)
    {
        if (damage <= 0) return new ReceivedDamageResult(0, false, false);
        if (stats == null) return new ReceivedDamageResult(damage, false, false);

        if (RollChance(stats.DodgeChance))
            return new ReceivedDamageResult(0, true, false);

        if (RollChance(stats.NegateDamageChance))
            return new ReceivedDamageResult(0, false, true);

        decimal value = ApplyDefense(damage, stats.FinalDefense);
        value = ApplyDamageReduction(value, stats.DamageReduction);

        int result = (int)Math.Round(value, MidpointRounding.ToEven);
        return new ReceivedDamageResult(Math.Max(result, MinReceivedDamage), false, false);
    }

    // 방어력 비율 감소만 (반올림·최소 피해는 CalculateReceived에서)
    public static decimal ApplyDefense(decimal damage, int defense)
        => defense <= 0 ? damage : damage * DefenseConstant / (DefenseConstant + defense);

    // 받는 피해 감소: × (100 − 감소%) ÷ 100 (감소%는 0~100으로 제한)
    public static decimal ApplyDamageReduction(decimal damage, int reductionPercent)
    {
        int reduction = Math.Clamp(reductionPercent, 0, 100);
        return damage * (100 - reduction) / 100;
    }

    // ===== 회복 =====

    // 받는 치유량 보정: 회복량 × (100 + 치유량%) ÷ 100, 버림, 최소 1
    public static int ApplyHealReceived(int amount, int healReceivedPercent)
    {
        if (amount <= 0) return 0;
        int result = amount * (100 + Math.Max(healReceivedPercent, 0)) / 100;
        return Math.Max(result, 1);
    }

    // 확률(%) 판정. 0 이하면 없음, 100 이상이면 항상
    private static bool RollChance(float chance)
    {
        if (chance <= 0f) return false;
        if (chance >= 100f) return true;
        return UnityEngine.Random.value * 100f < chance;
    }
}