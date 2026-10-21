using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase
{
    public const int MaxLevel = 5;

    protected SkillData skillData;
    protected int level = 0;

    protected GameObject prefab;
    protected GameObject evolvedPrefab;
    protected Transform transform;
    protected PlayerMovement movement;
    protected PlayerStats stats;
    protected SkillModifiers modifiers;

    // 이 스킬의 레벨 추가 효과 (SkillLevel 테이블)
    private IReadOnlyList<SkillLevelData> _levelBonuses = Array.Empty<SkillLevelData>();

    // 현재 레벨까지의 레벨 추가 효과 합계. 패시브 보너스(modifiers)와 따로 보관하므로 다른 스킬에는 영향 없음
    private readonly float[] _levelPercentTotals = new float[SkillModifiers.PercentStatCount];
    private readonly int[] _levelFlatTotals = new int[SkillModifiers.FlatStatCount];

    // 치명타 레벨 효과는 모든 스킬에 적용되는 플레이어 치명타이므로, 이 스킬을 출처로 SkillModifiers에 등록함
    // _levelAddTotals: 현재 레벨 합계, _registeredAddTotals: 마지막으로 SkillModifiers에 등록한 값 (달라졌을 때만 다시 등록)
    private readonly float[] _levelAddTotals = new float[SkillModifiers.AddStatCount];
    private readonly float[] _registeredAddTotals = new float[SkillModifiers.AddStatCount];

    public int Level => level;
    public bool IsEquipped { get; private set; }

    // 5레벨이 되고 짝 패시브를 가지면 진화함 (SkillEvolutionTable). 한 번 진화하면 유지됨
    public bool IsEvolved { get; private set; }

    public long SkillId => skillData != null ? skillData.skillId : -1;
    // 적 탐지 범위. 사거리증가 보너스 포함
    public float Range => skillData != null ? GetPercentStat(SkillPercentStat.Range, skillData.skillRange) : 0.0f;

    // 이번 발동의 대미지 비율 (반복으로 다시 발동한 스킬은 낮춤)
    protected virtual float DamageScale => 1.0f;

    // 퍼센트 증가 합계(레벨 효과 + 패시브)의 상한 (1.0 = +100%, 최대 2배). 쿨타임은 감소 상한(CooldownSkillBase.MaxCooldownReduction)을 따로 가지므로 제외
    private const float MaxPercentBonus = 1.0f;

    // 대미지 증가 합계(레벨 효과 + 패시브)에 더해지는 스킬 고유의 추가 증가 비율 (0.1 = +10%). 진화 효과처럼 스킬만의 대미지 증가가 있을 때 오버라이드
    protected virtual float ExtraDamagePercent => 0.0f;

    public virtual void Initialize(SkillData data)
    {
        skillData = data;
        level = 0;
        IsEvolved = false;

        _levelBonuses = GameManager.JsonData.GetSkillLevelsFromJson(data.skillId);
        RecalculateLevelBonuses();
    }

    public void SetContext(SkillContext context)
    {
        transform = context.Owner;
        movement = context.Movement;
        stats = context.Stats;
        modifiers = context.Modifiers;
    }

    // 프리팹이 필요 없는 스킬(패시브 등)은 null이 들어올 수 있음
    public void SetPrefab(GameObject prefab)
    {
        this.prefab = prefab;
    }

    // 진화했을 때 prefab 대신 소환할 프리팹. 없으면 진화해도 prefab을 씀
    public void SetEvolvedPrefab(GameObject evolvedPrefab)
    {
        this.evolvedPrefab = evolvedPrefab;
    }

    // 매 프레임 호출. 발동 방식은 스킬 유형마다 다르므로 서브클래스가 구현
    public abstract void Tick(float deltaTime);

    public void Equip()
    {
        IsEquipped = true;
        OnEquip();
    }

    public void Unequip()
    {
        IsEquipped = false;
        OnUnequip();

        // 이 스킬이 등록한 치명타 레벨 효과를 제거
        if(modifiers != null)
        {
            modifiers.RemoveSource(this);
            Array.Clear(_registeredAddTotals, 0, _registeredAddTotals.Length);
        }
    }

    public virtual void Levelup()
    {
        level = Math.Clamp(level + 1, 1, MaxLevel);
        RecalculateLevelBonuses();
        NotifyLevelChanged();
    }

    // 진화 조건을 만족했을 때 SkillController가 부름. 이미 진화한 스킬은 무시
    public void Evolve()
    {
        if(IsEvolved)
        {
            return;
        }

        IsEvolved = true;
        OnEvolved();
    }

    // 진화했을 때 스킬별 효과를 적용하는 곳. 진화 효과가 없는 스킬은 오버라이드하지 않음
    protected virtual void OnEvolved()
    {
    }

    public virtual void LevelDown()
    {
        level = Math.Clamp(level - 1, 1, MaxLevel);
        RecalculateLevelBonuses();
        NotifyLevelChanged();
    }

    // 현재 레벨 이하의 추가 효과를 모두 합산. 레벨이 바뀔 때만 계산하므로 발동할 때마다 목록을 돌지 않음
    private void RecalculateLevelBonuses()
    {
        Array.Clear(_levelPercentTotals, 0, _levelPercentTotals.Length);
        Array.Clear(_levelFlatTotals, 0, _levelFlatTotals.Length);
        Array.Clear(_levelAddTotals, 0, _levelAddTotals.Length);

        foreach(SkillLevelData bonus in _levelBonuses)
        {
            // 레벨 오름차순이라 현재 레벨을 넘으면 뒤는 볼 필요 없음
            if(bonus.skillLevel > level)
            {
                break;
            }

            AddLevelBonus(bonus.Effect1Type, bonus.effect1Value);
            AddLevelBonus(bonus.Effect2Type, bonus.effect2Value);
        }

        RegisterLevelAddBonuses();
    }

    // 테이블의 효과 종류를 스킬 스탯으로 바꿔 더함
    // 퍼센트형은 테이블에 5(= 5%)처럼 적으므로 여기서 비율(0.05)로 바꾸고, 테이블은 모두 양수로 적으므로 감소 효과는 부호를 뒤집음
    private void AddLevelBonus(SkillLevelStatType statType, float value)
    {
        const float PercentToRatio = 0.01f;

        switch(statType)
        {
            case SkillLevelStatType.Damage:
                _levelPercentTotals[(int)SkillPercentStat.Damage] += value * PercentToRatio;
                break;
            case SkillLevelStatType.Area:
                _levelPercentTotals[(int)SkillPercentStat.Area] += value * PercentToRatio;
                break;
            case SkillLevelStatType.Speed:
                _levelPercentTotals[(int)SkillPercentStat.Speed] += value * PercentToRatio;
                break;
            case SkillLevelStatType.CooldownReduction:
                _levelPercentTotals[(int)SkillPercentStat.Cooldown] -= value * PercentToRatio;
                break;
            case SkillLevelStatType.Range:
                _levelPercentTotals[(int)SkillPercentStat.Range] += value * PercentToRatio;
                break;
            case SkillLevelStatType.Pierce:
                _levelFlatTotals[(int)SkillFlatStat.Pierce] += Mathf.RoundToInt(value);
                break;
            case SkillLevelStatType.Chain:
                _levelFlatTotals[(int)SkillFlatStat.Chain] += Mathf.RoundToInt(value);
                break;
            case SkillLevelStatType.Repeat:
                _levelFlatTotals[(int)SkillFlatStat.Repeat] += Mathf.RoundToInt(value);
                break;
            case SkillLevelStatType.CriticalChance:
                _levelAddTotals[(int)SkillAddStat.CritChance] += value;
                break;
            case SkillLevelStatType.CriticalDamage:
                _levelAddTotals[(int)SkillAddStat.CritDamage] += value;
                break;
        }
    }

    // 치명타 레벨 효과는 SkillModifiers에 이 스킬을 출처로 등록. 치명타 판정이 플레이어 전체 기준이라 모든 스킬의 치명타에 적용됨
    // SkillController가 합계를 PlayerStats로 넘기고, 스킬이 해제되면 Unequip에서 제거됨
    private void RegisterLevelAddBonuses()
    {
        // 컨텍스트가 없으면 등록할 곳이 없음 (SetContext 이후 레벨이 바뀔 때 등록됨)
        if(modifiers == null)
        {
            return;
        }

        for(int i = 0; i < _levelAddTotals.Length; i++)
        {
            if(Mathf.Approximately(_registeredAddTotals[i], _levelAddTotals[i]))
            {
                continue;
            }

            _registeredAddTotals[i] = _levelAddTotals[i];
            modifiers.SetAdd(this, (SkillAddStat)i, _levelAddTotals[i]);
        }
    }

    // 장착된 상태에서 레벨이 바뀔 때만 훅 호출 (장착 전 최초 레벨 설정은 OnEquip에서 처리)
    private void NotifyLevelChanged()
    {
        if(IsEquipped)
        {
            OnLevelChanged();
        }
    }

    protected virtual void OnEquip()
    {
    }

    protected virtual void OnUnequip()
    {
    }

    protected virtual void OnLevelChanged()
    {
    }

    // 기본값에 이 스킬의 레벨 추가 효과와 패시브 보너스를 적용한 최종값. 보너스가 없으면 기본값 그대로
    // 두 보너스는 합산한 뒤 한 번에 곱함 (레벨 +5%, 패시브 +10% → 기본값 × 1.15)
    protected float GetPercentStat(SkillPercentStat stat, float baseValue)
    {
        return baseValue * (1.0f + GetPercentTotal(stat));
    }

    // 기본값에 곱하기 전의 비율 합계 (레벨 추가 효과 + 패시브 보너스). 감소 효과는 음수
    // 상한처럼 합계 자체에 규칙을 두어야 하는 스탯(쿨타임)이 직접 읽음
    protected float GetPercentTotal(SkillPercentStat stat)
    {
        float total = _levelPercentTotals[(int)stat];

        if(modifiers != null)
        {
            total += modifiers.GetPercentTotal(stat);
        }

        // 쿨타임을 제외한 증가 스탯(대미지, 범위, 속도, 사거리)은 합계가 MaxPercentBonus를 넘지 못함
        if(stat != SkillPercentStat.Cooldown)
        {
            total = Mathf.Min(total, MaxPercentBonus);
        }

        return total;
    }

    // 개수처럼 고정값으로 늘어나는 스탯용. 0 미만은 0으로 맞춤
    protected int GetFlatStat(SkillFlatStat stat, int baseValue)
    {
        int value = baseValue + _levelFlatTotals[(int)stat];

        if(modifiers != null)
        {
            value += modifiers.GetFlatTotal(stat);
        }

        return Mathf.Max(value, 0);
    }

    // 스킬 오브젝트에 넘길 최종 수치 계산 (SkillLevel 테이블의 레벨 효과, 패시브 보너스 반영)
    protected SkillStats BuildStats()
    {
        // 반올림하지 않고 배율 그대로 넘김 (공격력, 치명타, 반올림은 맞힐 때 PlayerDamageCalculator가 처리)
        // 스킬 고유의 추가 증가까지 합산한 뒤에 상한을 적용함 (합계가 MaxPercentBonus를 넘지 못함)
        float damageBonus = Mathf.Min(GetPercentTotal(SkillPercentStat.Damage) + ExtraDamagePercent, MaxPercentBonus);
        float skillMultiplier = skillData.skillDamage * (1.0f + damageBonus) * DamageScale;
        float speed = GetPercentStat(SkillPercentStat.Speed, skillData.skillSpeed);
        float scale = GetPercentStat(SkillPercentStat.Area, 1.0f);

        return new SkillStats(SkillId, skillMultiplier, speed, scale, stats, IsEvolved, Range);
    }

    // 풀에서 스킬 오브젝트를 꺼내 초기화. 초기화 인자 구조체 타입(TInitData)은 호출부에서 추론됨
    // 진화했고 진화용 프리팹이 있으면 그 프리팹을, 아니면 기본 프리팹을 소환함
    protected GameObject Spawn<TInitData>(Vector3 position, TInitData initData)
    {
        GameObject spawnPrefab = IsEvolved && evolvedPrefab != null ? evolvedPrefab : prefab;

        return Spawn(spawnPrefab, position, initData);
    }

    // 소환할 프리팹을 직접 지정하는 버전. 진화 후에도 기본 프리팹을 계속 쓰면서 진화 프리팹을 따로 소환하는 스킬(멀티탭 등)이 씀
    protected GameObject Spawn<TInitData>(GameObject spawnPrefab, Vector3 position, TInitData initData)
    {
        if(spawnPrefab == null)
        {
            Debug.LogWarning($"[SkillBase] 프리팹이 없습니다: {SkillId}");
            return null;
        }

        return SkillObjectPool.Instance.Get(spawnPrefab, position, Quaternion.identity, instance =>
        {
            if(instance.TryGetComponent(out SkillObject<TInitData> skillObject))
            {
                skillObject.Init(initData);
            }
        });
    }
}
