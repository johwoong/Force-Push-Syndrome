using System.Collections.Generic;
using UnityEngine;

// 탐지 범위 안의 모든 적에게 번개(LightningObject)를 하나씩 소환. 번개가 생기는 순간 대미지가 들어감
// 연쇄 패시브가 있으면 맞은 적에게서 아직 안 맞은 가까운 적에게 번개가 이어짐
// 발동 조건(쿨타임, 범위 안에 적이 있는지)은 CooldownSkillBase를 따름
// 진화: 기본 번개는 그대로 두고, 발동할 때마다 탐지 범위 안의 무작위 적에게 낙뢰(LightningStrikeObject)를 추가로 내리침 (케넨 궁극기처럼)
//       낙뢰는 기본 번개와 같은 수치(대미지, 범위 증가)의 단일 대상 공격이고 연쇄는 적용되지 않음
//       낙뢰 수 = 1 + 연쇄 횟수. 한 번의 발동에서 같은 적을 두 번 맞히지 않으므로 범위 안에 적이 적으면 그만큼만 내리침
//       기본 번개와 같은 발동 안에서 정해지므로 쿨타임과 반복(반복 발동은 대미지 50%)을 그대로 따름
//       낙뢰는 기본 번개보다 StrikeDelay만큼 늦게 내리쳐서, 두 대미지가 시간차를 두고 들어가 따로 보이게 함
//       낙뢰는 SkillPrefabTable의 EvolvedPrefab으로 소환함 (LightningStrikeObject가 붙은 프리팹). 기본 번개는 진화 후에도 기본 프리팹으로 소환함
public sealed class Skill_PowerStrip : CooldownSkillBase
{
    // 연쇄 번개가 다음 적을 찾는 반경 (직전에 맞은 적 기준). 임시 수치
    private const float ChainRadius = 3.0f;

    // 장착할 때 미리 만들어 둘 번개 수 (첫 발동에 Instantiate가 몰리지 않도록). 임시 수치
    private const int PrewarmCount = 64;

    // 진화할 때 미리 만들어 둘 낙뢰 수. 임시 수치
    private const int StrikePrewarmCount = 16;

    // 기본 번개가 나간 뒤 낙뢰가 소환되기까지의 시간 (초). 두 대미지가 따로 보이도록 하는 간격. 임시 수치
    // 낙뢰 프리팹의 _hitDelay(소환 후 대미지까지)는 이 시간에 더해짐
    private const float StrikeDelay = 0.3f;

    // 낙뢰 소환을 기다리는 중인 대상. 발동 시점의 수치(대미지 비율 등)를 그대로 가져감
    private struct PendingStrike
    {
        public Enemy Enemy;
        public SkillStats Stats;
        public float Timer;
    }

    private readonly List<PendingStrike> _pendingStrikes = new();

    // 이번 발동에서 낙뢰를 맞을 적 (탐지 범위 안에서 무작위로 고름)
    private readonly List<Enemy> _strikeTargets = new();

    // 진화 프리팹이 비어 있다는 경고를 한 번만 띄우기 위함
    private bool _isStrikeUnavailable;

    // 이번 발동에서 번개를 맞을 수 있는 적. 연쇄 대상을 물리 탐색 없이 이 목록에서 거리 비교로 찾음
    private struct Candidate
    {
        public Enemy Enemy;
        public Vector2 Position;
        public bool IsStruck;
    }

    private readonly List<Candidate> _candidates = new();

    // _candidates 중 탐지 범위 안에 있어 처음 번개를 맞는 적의 인덱스
    private readonly List<int> _initialIndices = new();

    // 콜라이더가 여러 개인 적이 후보에 중복으로 들어가지 않도록 함
    private readonly HashSet<Enemy> _registeredEnemies = new();

    protected override void OnEquip()
    {
        base.OnEquip();

        SkillObjectPool.Instance.Prewarm(prefab, PrewarmCount);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if(_pendingStrikes.Count > 0)
        {
            TickPendingStrikes(deltaTime);
        }
    }

    protected override void OnUnequip()
    {
        base.OnUnequip();
        _pendingStrikes.Clear();
    }

    // 진화할 때 낙뢰를 미리 만들어 둠
    protected override void OnEvolved()
    {
        if(evolvedPrefab != null)
        {
            SkillObjectPool.Instance.Prewarm(evolvedPrefab, StrikePrewarmCount);
        }
    }

    protected override void Fire()
    {
        // 번개마다 같은 수치를 쓰므로 한 번만 계산
        SkillStats skillStats = BuildStats();
        int chainCount = GetFlatStat(SkillFlatStat.Chain, 0);

        CollectCandidates(chainCount);

        // 번개 대미지로 적이 죽기 전에 낙뢰 대상을 먼저 정함
        if(IsEvolved)
        {
            SelectStrikeTargets(1 + chainCount);
        }

        foreach(int index in _initialIndices)
        {
            Spawn(prefab, transform.position, new LinkData(skillStats, transform, _candidates[index].Enemy.transform));

            if(chainCount > 0)
            {
                Chain(index, chainCount, skillStats);
            }
        }

        if(IsEvolved)
        {
            Strike(skillStats);
        }
    }

    // 물리 탐색은 최대 두 번만 함: 탐지 범위(처음 대상), 연쇄가 닿을 수 있는 최대 범위(연쇄 후보)
    private void CollectCandidates(int chainCount)
    {
        _candidates.Clear();
        _initialIndices.Clear();
        _registeredEnemies.Clear();

        float range = Range;

        // 공유 버퍼를 순회하는 중에는 다시 탐색하지 않음 (순회가 끝난 뒤 다음 탐색)
        foreach(Collider2D other in SkillTargeting.OverlapEnemies(transform.position, range))
        {
            if(TryRegister(other, true))
            {
                _initialIndices.Add(_candidates.Count - 1);
            }
        }

        if(chainCount <= 0 || _initialIndices.Count == 0)
        {
            return;
        }

        // 연쇄는 탐지 범위 끝의 적에서 시작해 한 번에 ChainRadius씩 뻗어 나가므로 그 바깥은 닿을 수 없음
        float chainReach = range + ChainRadius * chainCount;

        foreach(Collider2D other in SkillTargeting.OverlapEnemies(transform.position, chainReach))
        {
            TryRegister(other, false);
        }
    }

    // 살아 있고 아직 등록되지 않은 적이면 후보에 추가. isStruck은 처음 번개를 맞는 대상인지
    private bool TryRegister(Collider2D other, bool isStruck)
    {
        if(!other.TryGetComponent(out Enemy enemy) || enemy.isDead || !_registeredEnemies.Add(enemy))
        {
            return false;
        }

        _candidates.Add(new Candidate
        {
            Enemy = enemy,
            Position = enemy.transform.position,
            IsStruck = isStruck
        });

        return true;
    }

    // fromIndex의 적에서 시작해 chainCount번 다음 적으로 번개를 이음. 이을 적이 없으면 거기서 멈춤
    private void Chain(int fromIndex, int chainCount, SkillStats skillStats)
    {
        int currentIndex = fromIndex;

        for(int i = 0; i < chainCount; i++)
        {
            int nextIndex = FindNearestCandidate(_candidates[currentIndex].Position);

            if(nextIndex < 0)
            {
                return;
            }

            Candidate next = _candidates[nextIndex];
            next.IsStruck = true;
            _candidates[nextIndex] = next;

            Transform from = _candidates[currentIndex].Enemy.transform;
            Spawn(prefab, _candidates[currentIndex].Position, new LinkData(skillStats, from, next.Enemy.transform));

            currentIndex = nextIndex;
        }
    }

    // position에서 ChainRadius 안에 있고 아직 번개를 맞지 않은 가장 가까운 후보의 인덱스. 없으면 -1
    private int FindNearestCandidate(Vector2 position)
    {
        int nearestIndex = -1;
        float minSqrDistance = ChainRadius * ChainRadius;

        for(int i = 0; i < _candidates.Count; i++)
        {
            Candidate candidate = _candidates[i];

            // 앞선 번개에 죽은 적은 건너뜀
            if(candidate.IsStruck || candidate.Enemy.isDead)
            {
                continue;
            }

            float sqrDistance = (candidate.Position - position).sqrMagnitude;

            if(sqrDistance <= minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    // 탐지 범위 안(_initialIndices)의 살아 있는 적 중 무작위로 최대 count명을 _strikeTargets에 담음
    // 같은 적은 한 번만 담기므로 적이 count명보다 적으면 그만큼만 담김
    private void SelectStrikeTargets(int count)
    {
        _strikeTargets.Clear();

        foreach(int index in _initialIndices)
        {
            _strikeTargets.Add(_candidates[index].Enemy);
        }

        // 앞에서부터 count자리만 무작위로 채우면 되므로 부분 셔플 (뽑은 적은 앞자리에 두어 다시 뽑히지 않게 함)
        int pickCount = Mathf.Min(count, _strikeTargets.Count);

        for(int i = 0; i < pickCount; i++)
        {
            int swapIndex = Random.Range(i, _strikeTargets.Count);

            (_strikeTargets[i], _strikeTargets[swapIndex]) = (_strikeTargets[swapIndex], _strikeTargets[i]);
        }

        if(_strikeTargets.Count > pickCount)
        {
            _strikeTargets.RemoveRange(pickCount, _strikeTargets.Count - pickCount);
        }
    }

    // SelectStrikeTargets로 정한 적에게 낙뢰를 소환
    private void Strike(SkillStats skillStats)
    {
        if(_strikeTargets.Count == 0)
        {
            return;
        }

        if(evolvedPrefab == null)
        {
            if(!_isStrikeUnavailable)
            {
                _isStrikeUnavailable = true;
                Debug.LogWarning($"[Skill_PowerStrip] 진화 프리팹(SkillPrefabTable의 EvolvedPrefab)이 비어 있어 낙뢰가 나가지 않습니다: {SkillId}");
            }

            return;
        }

        // 바로 소환하지 않고 StrikeDelay 뒤에 소환 (기본 번개와 시간차를 둠)
        foreach(Enemy target in _strikeTargets)
        {
            _pendingStrikes.Add(new PendingStrike
            {
                Enemy = target,
                Stats = skillStats,
                Timer = StrikeDelay
            });
        }
    }

    // 기다리던 낙뢰의 시간이 되면 소환. 그 사이 대상이 죽었으면 소환하지 않음
    private void TickPendingStrikes(float deltaTime)
    {
        for(int i = _pendingStrikes.Count - 1; i >= 0; i--)
        {
            PendingStrike pending = _pendingStrikes[i];

            pending.Timer -= deltaTime;

            if(pending.Timer > 0.0f)
            {
                _pendingStrikes[i] = pending;
                continue;
            }

            _pendingStrikes.RemoveAt(i);

            if(pending.Enemy != null && !pending.Enemy.isDead && pending.Enemy.gameObject.activeInHierarchy)
            {
                Spawn(evolvedPrefab, pending.Enemy.transform.position, new StrikeData(pending.Stats, pending.Enemy.transform));
            }
        }
    }
}
