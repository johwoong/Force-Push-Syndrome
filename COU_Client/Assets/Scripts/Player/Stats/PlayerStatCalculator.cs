using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 플레이어 최종 스탯 계산기 (플레이어 오브젝트 없이도 사용 가능)
// 전투(PlayerStats)와 장비창이 같은 계산을 쓰도록 한곳에 둔다
// 최종 = (기본 + 진화 + 장비 합계) × (100 + 보너스%) / 100, 버림
public static class PlayerStatCalculator
{
    public struct Result
    {
        public int FinalAttack;
        public int FinalHp;
        public int FinalDefense;
        public int BaseAttack;
        public int BaseHp;
        public int EvolutionAttack;
        public int EvolutionHp;
        public int EquipmentAttack;
        public int EvolutionDefense;
        public int EquipmentHp;
        public bool HasEvolutionData;
        public int CombatPower;
        public bool HasCombatPowerData;
        public int EvolutionPotionRecovery;
        public int FinalPotionRecovery;
        public int EquipmentMoveSpeedPercent;
        public float FinalMoveSpeed;

        // 장비 등급 효과
        public EquipmentEffectSummary Effects;   // 합산 결과 전체 (치명타·회피 등은 C4·C5에서 사용)
        public int AttackBonusPercent;           // = Effects.AttackPercent
        public int HpBonusPercent;               // = Effects.HpPercent
        public int EquipmentDefense;             // = Effects.Defense
        public int EffectMoveSpeedPercent;       // = Effects.MoveSpeedPercent
    }


    public static Result Calculate(PlayerBaseStatData baseStat, EvolutionData evolution,
                               PlayerStatData playerStat, IReadOnlyCollection<OwnedItem> equipped)
    {
        var effects = EquipmentEffectResolver.Resolve(equipped);

        var result = new Result
        {
            BaseAttack = baseStat.playerBaseAttack,
            BaseHp = baseStat.playerBaseHp,
            EvolutionAttack = EvolutionRules.GetBonus(evolution, playerStat, PlayerStatType.Attack),
            EvolutionHp = EvolutionRules.GetBonus(evolution, playerStat, PlayerStatType.Hp),
            EquipmentMoveSpeedPercent = equipped.Sum(item => item.Data.moveSpeedBonus),
            EquipmentAttack = equipped.Sum(item => item.ScaledAttack),
            EquipmentHp = equipped.Sum(item => item.ScaledHp),
            HasEvolutionData = evolution != null,

            Effects = effects,
            AttackBonusPercent = effects.AttackPercent,
            HpBonusPercent = effects.HpPercent,
            EquipmentDefense = effects.Defense,
            EffectMoveSpeedPercent = effects.MoveSpeedPercent
        };

        // 최종 = (기본 + 진화 + 장비 고정값) × (100 + 보너스%) / 100, 버림 (서버와 동일)
        result.FinalAttack = ApplyBonus(result.BaseAttack + result.EvolutionAttack + result.EquipmentAttack, result.AttackBonusPercent);
        result.FinalHp = ApplyBonus(result.BaseHp + result.EvolutionHp + result.EquipmentHp, result.HpBonusPercent);

        // 방어력 = 진화 + 장비(등급 효과 합)
        result.EvolutionDefense = EvolutionRules.GetBonus(evolution, playerStat, PlayerStatType.Defense);
        result.FinalDefense = result.EvolutionDefense + result.EquipmentDefense;

        result.EvolutionPotionRecovery = EvolutionRules.GetBonus(evolution, playerStat, PlayerStatType.PotionRecovery);
        result.FinalPotionRecovery = result.EvolutionPotionRecovery;

        // 이동속도 = 기본 × (100 + 장비 moveSpeedBonus 합 + 등급 효과 이동속도%) / 100, 상한
        int moveSpeedPercent = result.EquipmentMoveSpeedPercent + result.EffectMoveSpeedPercent;
        float moveSpeed = baseStat.playerBaseMoveSpeed * (100 + moveSpeedPercent) / 100f;
        result.FinalMoveSpeed = Mathf.Min(moveSpeed, baseStat.playerBaseMaxMoveSpeed);

        // 전투력 (랭킹용, 서버와 같은 정수 계산)
        var combatPower = GameManager.JsonData != null ? GameManager.JsonData.CombatPowerData : null;
        result.HasCombatPowerData = combatPower != null;
        result.CombatPower = CalculateCombatPower(combatPower, result);

        return result;
    }

    public static Result CalculateCurrent()
    {
        var data = GameManager.PlayerData.currentData;
        return Calculate(GameManager.JsonData.PlayerBaseStatData, EvolutionRules.Data,
                         data.playerStat, GetEquipped(data));
    }

    public static List<OwnedItem> GetEquipped(PlayerSaveData data)
    {
        var result = new List<OwnedItem>();

        if (PlayerInventory.Instance != null)
        {
            foreach (EquipSlotType slot in Enum.GetValues(typeof(EquipSlotType)))
            {
                var item = PlayerInventory.Instance.GetEquipped(slot);
                if (item != null)
                    result.Add(item);
            }

            return result;
        }

        InventoryRestorer.Restore(data.inventoryList, out var equipped);
        result.AddRange(equipped.Values);
        return result;
    }

    // 전투력 = 공격력 × 가중치 + 체력 × 가중치 + 방어력 × 가중치 (정수만, 서버와 동일)
    public static int CalculateCombatPower(CombatPowerData data, Result stat)
    {
        if (data == null) return 0;

        if (data.combatPowerUseCritical)
            UnityEngine.Debug.LogWarning("[PlayerStatCalculator] 치명타 반영 전투력은 아직 지원하지 않습니다. 치명타 없이 계산합니다.");

        return stat.FinalAttack * data.combatPowerAttackWeight
             + stat.FinalHp * data.combatPowerHpWeight
             + stat.FinalDefense * data.combatPowerDefenseWeight;
    }

    private static int ApplyBonus(int sum, int bonusPercent)
        => sum * (100 + bonusPercent) / 100;
}
