using System;
using System.Collections.Generic;

// 퍼센트로 증감하는 스탯. 값은 비율로 넣음 (0.1 = +10%, -0.2 = -20%)
// 기본값 × (1 + 합계)로 계산하므로 감소 효과(쿨타임감소 등)는 음수로 등록해야 함
public enum SkillPercentStat
{
    Damage,
    Cooldown,
    Area,   // 크기 (기본 1.0배)
    Speed,
    Range   // 탐지 범위
}

// 고정 수치로 증감하는 스탯
public enum SkillFlatStat
{
    ProjectileCount, // 투사체/지속형 오브젝트 개수
    Pierce,
    Chain,           // 맞힌 뒤 주변 적에게 튕기는 횟수
    Repeat           // 텀을 두고 스킬을 다시 발동하는 횟수
}

// 기본값에 그대로 더하는 스탯 (20 = +20)
public enum SkillAddStat
{
    CritChance,  // 치명타율 (퍼센트 포인트)
    CritDamage   // 치명타증폭 (퍼센트 포인트)
}

// 패시브가 등록하고, 액티브 스킬이 발동할 때 읽는 보너스 모음
// 보너스를 등록한 주체(source)별로 보관해서 해제할 때 수치를 되돌릴 필요 없이 통째로 제거함
public class SkillModifiers
{
    public static readonly int PercentStatCount = Enum.GetValues(typeof(SkillPercentStat)).Length;
    public static readonly int FlatStatCount = Enum.GetValues(typeof(SkillFlatStat)).Length;
    public static readonly int AddStatCount = Enum.GetValues(typeof(SkillAddStat)).Length;

    private sealed class SourceEntry
    {
        public readonly float[] Percents = new float[PercentStatCount];
        public readonly int[] Flats = new int[FlatStatCount];
        public readonly float[] Adds = new float[AddStatCount];
    }

    private readonly Dictionary<SkillBase, SourceEntry> _entriesBySource = new();

    // 종류별 합계 캐시 (Apply가 발사할 때마다 불리므로 변경 시점에만 다시 계산)
    private readonly float[] _percentTotals = new float[PercentStatCount];
    private readonly int[] _flatTotals = new int[FlatStatCount];
    private readonly float[] _addTotals = new float[AddStatCount];

    // 보너스가 바뀔 때 호출 (지속형 스킬의 오브젝트 재생성, 치명타 보너스 전달 등에 사용)
    public event Action OnChanged;

    // source가 등록한 같은 종류의 보너스를 새 값으로 교체
    public void SetPercent(SkillBase source, SkillPercentStat stat, float value)
    {
        GetEntry(source).Percents[(int)stat] = value;
        Recalculate();
    }

    public void SetFlat(SkillBase source, SkillFlatStat stat, int value)
    {
        GetEntry(source).Flats[(int)stat] = value;
        Recalculate();
    }

    public void SetAdd(SkillBase source, SkillAddStat stat, float value)
    {
        GetEntry(source).Adds[(int)stat] = value;
        Recalculate();
    }

    public void RemoveSource(SkillBase source)
    {
        if(_entriesBySource.Remove(source))
        {
            Recalculate();
        }
    }

    // 최종값은 스킬의 레벨 추가 효과와 합쳐야 하므로 SkillBase.GetPercentStat/GetFlatStat에서 계산함
    public float GetPercentTotal(SkillPercentStat stat)
    {
        return _percentTotals[(int)stat];
    }

    public int GetFlatTotal(SkillFlatStat stat)
    {
        return _flatTotals[(int)stat];
    }

    public float GetAddTotal(SkillAddStat stat)
    {
        return _addTotals[(int)stat];
    }

    private SourceEntry GetEntry(SkillBase source)
    {
        if(!_entriesBySource.TryGetValue(source, out SourceEntry entry))
        {
            entry = new SourceEntry();
            _entriesBySource[source] = entry;
        }

        return entry;
    }

    private void Recalculate()
    {
        Array.Clear(_percentTotals, 0, _percentTotals.Length);
        Array.Clear(_flatTotals, 0, _flatTotals.Length);
        Array.Clear(_addTotals, 0, _addTotals.Length);

        foreach(SourceEntry entry in _entriesBySource.Values)
        {
            for(int i = 0; i < PercentStatCount; i++)
            {
                _percentTotals[i] += entry.Percents[i];
            }

            for(int i = 0; i < FlatStatCount; i++)
            {
                _flatTotals[i] += entry.Flats[i];
            }

            for(int i = 0; i < AddStatCount; i++)
            {
                _addTotals[i] += entry.Adds[i];
            }
        }

        OnChanged?.Invoke();
    }
}
