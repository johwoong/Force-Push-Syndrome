using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 플레이어 스탯
// 최종 = (기본 + 진화 + 장비 합계) × (100 + 보너스%) / 100, 버림
// - 기본: PlayerBaseStat (정적 데이터)
// - 진화: 계정 데이터의 진화 레벨 × Evolution 증가량 (정적 데이터). 데이터가 없으면 0
// - 장비: 장착 장비의 레벨·등급 반영 스탯 합 (클라이언트 계산)
public class PlayerStats : MonoBehaviour
{
    private bool isCalculated;
    private int finalAtk;
    private int finalHp;
    private int finalDefense;
    private int finalPotionRecovery;
    private float finalMoveSpeed;
    private bool hasWeapon;
    private string equippedWeaponName;
    private long? equippedWeaponItemId;
    public long? EquippedWeaponItemId { get { EnsureCalculated(); return equippedWeaponItemId; } }   // 무기가 없으면 null
    private long startingSkillId;

    // 장비 등급 효과 (전투 시작 시 고정)
    private int equipmentCriticalChance;
    private int equipmentCriticalDamage;
    private int dodgeChance;
    private int damageReduction;
    private int healReceived;
    private float negateDamageChance;
    private float intervalHealPercent;
    private float intervalHealSeconds;

    private static PlayerBaseStatData Base => GameManager.JsonData.PlayerBaseStatData;

    public int FinalAtk { get { EnsureCalculated(); return finalAtk; } }
    public int FinalHp { get { EnsureCalculated(); return finalHp; } }
    public int FinalDefense { get { EnsureCalculated(); return finalDefense; } }
    public int FinalPotionRecovery { get { EnsureCalculated(); return finalPotionRecovery; } }
    public bool HasWeapon { get { EnsureCalculated(); return hasWeapon; } }
    public string EquippedWeaponName { get { EnsureCalculated(); return equippedWeaponName; } }
    public long StartingSkillId { get { EnsureCalculated(); return startingSkillId; } }   // 0이면 시작 스킬 없음

    // 기본 + 보너스 합
    public int CriticalChance { get { EnsureCalculated(); return Base.playerBaseCriticalChance + equipmentCriticalChance + GetBonus(PlayerBonusStat.CriticalChance); } }
    public int CriticalDamage { get { EnsureCalculated(); return Base.playerBaseCriticalDamage + equipmentCriticalDamage + GetBonus(PlayerBonusStat.CriticalDamage); } }

    // 장비 등급 효과 신규 스탯
    public int DodgeChance { get { EnsureCalculated(); return dodgeChance; } }              // 회피 확률 %
    public int DamageReduction { get { EnsureCalculated(); return damageReduction; } }      // 받는 피해 감소 %
    public int HealReceived { get { EnsureCalculated(); return healReceived; } }            // 받는 치유량 +%
    public float NegateDamageChance { get { EnsureCalculated(); return negateDamageChance; } }   // 피격 시 피해 무효 확률 %
    public float IntervalHealPercent { get { EnsureCalculated(); return intervalHealPercent; } } // 주기 회복: 최대 체력의 %
    public float IntervalHealSeconds { get { EnsureCalculated(); return intervalHealSeconds; } } // 주기 회복 간격 (0이면 없음)
    public int SkillDamage => Base.playerBaseSkillDamage;
    public float Speed { get { EnsureCalculated(); return finalMoveSpeed; } }
    public float LootRadius => Base.playerBaseLootRadius;

    // 출처(패시브 등)별 보너스. 같은 출처가 다시 등록하면 덮어씀
    private readonly Dictionary<object, Dictionary<PlayerBonusStat, int>> bonuses = new();

    // 보너스가 바뀌었을 때 (치명타 표시 UI 등에서 구독 가능)
    public event Action OnBonusChanged;

    private void Start()
    {
        EnsureCalculated();
    }

    // 로그용: 기준 피해(배율 1.0)와 시작 스킬 Lv1 피해 (치명타 판정 없이 일반·치명타 값)
    private string GetDamageLog()
    {
        string log = "\n / 치명타 " + CriticalChance + "% (" + CriticalDamage + "%)" +
                     " / 회피 " + dodgeChance + "% / 받는 피해 -" + damageReduction + "% / 치유량 +" + healReceived + "%" +
                     " / 피해 무효 " + negateDamageChance + "% / 주기 회복 " + intervalHealPercent + "% (" + intervalHealSeconds + "초)";

        var basic = PlayerDamageCalculator.Preview(1f, this);
        log += "\n / 피해(배율 1.0) " + basic.normal + " (치명타 " + basic.critical + ", " + CriticalChance + "%)";

        if (startingSkillId == WeaponSkillTable.NoSkill)
            return log;

        var skillData = GameManager.JsonData.GetSkillDataFromJson(startingSkillId);
        if (skillData == null)
            return log;

        // 레벨 1, 패시브 없음 기준이라 스킬 배율 = skillDamage
        var skill = PlayerDamageCalculator.Preview(skillData.skillDamage, this);
        return log + " / 시작 스킬 Lv1 피해 " + skill.normal + " (치명타 " + skill.critical + ")";
    }

    // 누가 먼저 읽든 처음 한 번만 실행
    private void EnsureCalculated()
    {
        if (isCalculated) return;
        isCalculated = true;

        startingSkillId = WeaponSkillTable.NoSkill;

        if (!IsPlayerDataLoaded())
        {
            HandleNotLoggedIn();
            return;
        }

        var data = GameManager.PlayerData.currentData;
        var equipped = PlayerStatCalculator.GetEquipped(data);
        var stat = PlayerStatCalculator.Calculate(Base, EvolutionRules.Data, data.playerStat, equipped);

        if (!stat.HasEvolutionData)
            Debug.LogWarning("[PlayerStats] 진화 데이터가 없어 진화 스탯을 0으로 계산합니다.");

        finalAtk = stat.FinalAttack;
        finalHp = stat.FinalHp;
        finalDefense = stat.FinalDefense;
        finalPotionRecovery = stat.FinalPotionRecovery;
        finalMoveSpeed = stat.FinalMoveSpeed;
        var effects = stat.Effects;
        equipmentCriticalChance = effects.CriticalChance;
        equipmentCriticalDamage = effects.CriticalDamage;
        dodgeChance = effects.DodgeChance;
        damageReduction = effects.DamageReduction;
        healReceived = effects.HealReceived;
        negateDamageChance = effects.NegateDamageChance;
        intervalHealPercent = effects.IntervalHealPercent;
        intervalHealSeconds = effects.IntervalHealSeconds;

        LoadWeapon(equipped);

        Debug.Log("[PlayerStats] 공격력 " + finalAtk + " ((기본 " + stat.BaseAttack + " + 진화 " + stat.EvolutionAttack + " + 장비 " + stat.EquipmentAttack + ") × " + (100 + stat.AttackBonusPercent) + "%)" +
          " / 체력 " + finalHp + " ((기본 " + stat.BaseHp + " + 진화 " + stat.EvolutionHp + " + 장비 " + stat.EquipmentHp + ") × " + (100 + stat.HpBonusPercent) + "%)" +
          " / 방어력 " + finalDefense + " (진화 " + stat.EvolutionDefense + " + 장비 " + stat.EquipmentDefense + ")" +
          " / 포션 회복량 +" + finalPotionRecovery +
          " / 이동 속도 " + finalMoveSpeed.ToString("F2") + " (+" + (stat.EquipmentMoveSpeedPercent + stat.EffectMoveSpeedPercent) + "%)" +
          " / 무기: " + (hasWeapon ? equippedWeaponName : "없음") +
          " / 시작 스킬: " + (startingSkillId == WeaponSkillTable.NoSkill ? "없음" : startingSkillId.ToString()) +
          GetDamageLog());
    }

    // 보너스 등록: 같은 출처·같은 스탯이면 덮어씀 (패시브 레벨업 때 다시 호출)
    public void SetBonus(object source, PlayerBonusStat stat, int value)
    {
        if (source == null) return;

        if (!bonuses.TryGetValue(source, out var map))
        {
            map = new Dictionary<PlayerBonusStat, int>();
            bonuses[source] = map;
        }

        map[stat] = value;
        OnBonusChanged?.Invoke();
    }

    // 출처의 보너스 전부 제거 (패시브 해제 시)
    public void ClearBonuses(object source)
    {
        if (source != null && bonuses.Remove(source))
            OnBonusChanged?.Invoke();
    }

    // 해당 스탯의 보너스 합
    public int GetBonus(PlayerBonusStat stat)
    {
        int sum = 0;
        foreach (var map in bonuses.Values)
        {
            if (map.TryGetValue(stat, out int value))
                sum += value;
        }
        return sum;
    }

    // ===== 테스트 (전투 중 Player의 PlayerStats ⋮ 메뉴) =====

    [ContextMenu("Test/치명타 확률 +50 보너스")]
    private void TestCriticalBonus() => SetBonus(this, PlayerBonusStat.CriticalChance, 50);

    [ContextMenu("Test/보너스 제거")]
    private void TestClearBonus() => ClearBonuses(this);

    [ContextMenu("Test/피해 계산 20회 (스킬 배율 1.0)")]
    private void TestDamage()
    {
        int critCount = 0;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 20; i++)
        {
            var hit = PlayerDamageCalculator.Calculate(1.0f, this);
            if (hit.isCritical) critCount++;
            sb.Append(hit.damage).Append(hit.isCritical ? "! " : " ");
        }
        Debug.Log("[PlayerStats] 공격력 " + FinalAtk + ", 치명타 " + CriticalChance + "% / " + CriticalDamage + "% → " + sb + " (치명타 " + critCount + "회)");
    }

    private void LoadWeapon(List<OwnedItem> equipped)
    {
        var weapon = equipped.Find(item => item.Data.SlotType == EquipSlotType.Weapon);
        hasWeapon = weapon != null;
        equippedWeaponItemId = hasWeapon ? weapon.itemId : null;
        if (!hasWeapon) return;

        equippedWeaponName = weapon.Data.itemName;
        startingSkillId = WeaponSkillTable.GetSkillByItem(weapon.itemId);

        if (startingSkillId == WeaponSkillTable.NoSkill)
            Debug.LogWarning("[PlayerStats] 무기 → 시작 스킬 매핑이 없습니다: " + equippedWeaponName + " (" + weapon.itemId + "). 스킬 없이 시작");
    }

    // 로그인 없이 전투에 들어온 경우: 에러 후 로그인 화면 (예외 없음 정책)
    private void HandleNotLoggedIn()
    {
        Debug.LogError("[PlayerStats] 로그인 데이터가 없습니다. 로그인 화면으로 이동합니다.");
        GameManager.Scene.ChangeScene(GameConstants.SceneNames.LOGIN_SCENE);
    }

    private static bool IsPlayerDataLoaded()
    {
        var dataManager = GameManager.PlayerData;
        return dataManager != null && dataManager.isPlayerDataLoaded;
    }
}