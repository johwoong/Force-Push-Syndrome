using UnityEngine;

// 레벨 배율과 패시브 보너스가 모두 반영된, 스킬 오브젝트가 그대로 쓰는 최종 수치
// SkillData(기획 원본 값)와 달리 스킬 클래스가 계산을 끝낸 값만 담음
// 대미지는 스킬 몫인 배율까지만 계산해 두고, 공격력/치명타/반올림은 맞힐 때 플레이어 쪽(PlayerDamageCalculator)에서 계산함
public readonly struct SkillStats
{
    public readonly long SkillId; // 피해를 준 스킬 (업적 '스킬로 처치' 집계용)
    public readonly float SkillMultiplier; // 반올림하지 않은 스킬 배율 (skillDamage × 레벨 배율 × 패시브)
    public readonly float Speed;
    public readonly float Scale; // 1.0 = 프리팹 원래 크기
    public readonly float Range; // 레벨 효과와 패시브가 반영된 최종 사거리. 투사체가 날아갈 수 있는 최대 거리의 기준 (0이면 모름)
    public readonly PlayerStats PlayerStats;
    public readonly bool IsEvolved; // 이 스킬이 진화한 상태인지. 진화 여부에 따라 다르게 동작하는 오브젝트가 읽음

    public SkillStats(long skillId, float skillMultiplier, float speed, float scale, PlayerStats playerStats, bool isEvolved = false, float range = 0.0f)
    {
        SkillId = skillId;
        SkillMultiplier = skillMultiplier;
        Speed = speed;
        Scale = scale;
        PlayerStats = playerStats;
        IsEvolved = isEvolved;
        Range = range;
    }

    // 적을 맞힐 때마다 호출. 치명타는 맞힐 때마다 따로 판정됨
    public PlayerDamageCalculator.HitResult CalculateHit()
    {
        return CalculateHit(1.0f);
    }

    // 스킬 배율의 damageRatio배만 주는 타격 (도트 필드의 틱 등). 1이면 일반 타격과 같음
    public PlayerDamageCalculator.HitResult CalculateHit(float damageRatio)
    {
        return PlayerDamageCalculator.Calculate(SkillMultiplier * damageRatio, PlayerStats);
    }
}

// 스킬 오브젝트(SkillObject<TInitData>)가 Init으로 받는 초기화 데이터 모음
// 스킬 유형마다 필요한 값이 달라 구조체를 나눠 두며, SkillBase.Spawn<TInitData>가 이 타입으로 대상 오브젝트를 찾음

// 투사체 (SkillProjectile)
public readonly struct ProjectileData
{
    public readonly SkillStats Stats;
    public readonly Vector2 Direction;
    public readonly Transform Target; // 직선 탄환은 null
    public readonly int PierceCount; // 맞힐 수 있는 적 수 (1 = 첫 적을 맞히면 사라짐)
    public readonly Transform Owner; // 돌아올 대상 (부메랑 등). 돌아오지 않는 탄환은 null
    public readonly int ChainCount;  // 관통을 다 쓴 뒤 주변 적에게 튕기는 횟수

    public ProjectileData(SkillStats stats, Vector2 direction, Transform target = null, int pierceCount = 1, Transform owner = null, int chainCount = 0)
    {
        Stats = stats;
        Direction = direction.normalized;
        Target = target;
        PierceCount = pierceCount;
        Owner = owner;
        ChainCount = chainCount;
    }
}

// 범위 효과
public readonly struct AoEData
{
    public readonly SkillStats Stats;
    public readonly Vector2 Local;
    public readonly Vector2 Direction; // 방향이 필요 없는 범위는 zero

    public AoEData(SkillStats stats, Vector2 local, Vector2 direction = default)
    {
        Stats = stats;
        Local = local;
        Direction = direction.normalized;
    }
}

// 플레이어 주위를 도는 오브젝트 (SpeakerObject)
public readonly struct OrbitData
{
    public readonly SkillStats Stats;
    public readonly Transform Owner;
    public readonly float StartAngle;
    public readonly float Radius;

    public OrbitData(SkillStats stats, Transform owner, float startAngle, float radius)
    {
        Stats = stats;
        Owner = owner;
        StartAngle = startAngle;
        Radius = radius;
    }
}

// 두 대상을 잇는 오브젝트 (LightningObject)
public readonly struct LinkData
{
    public readonly SkillStats Stats;
    public readonly Transform Owner;  // 시작점 (스킬 사용자)
    public readonly Transform Target; // 끝점 (대미지를 받을 적)

    public LinkData(SkillStats stats, Transform owner, Transform target)
    {
        Stats = stats;
        Owner = owner;
        Target = target;
    }
}

// 사용자를 따라다니며 바라보는 방향에 범위를 유지하는 오브젝트 (FacingFieldObject)
public readonly struct FacingFieldData
{
    public readonly SkillStats Stats;
    public readonly Transform Owner;          // 따라갈 대상 (스킬 사용자)
    public readonly PlayerMovement Movement;  // 바라보는 방향(FacingDirection)을 읽음

    public FacingFieldData(SkillStats stats, Transform owner, PlayerMovement movement)
    {
        Stats = stats;
        Owner = owner;
        Movement = movement;
    }
}

// 대상 적 하나의 위치에 내리치는 오브젝트 (LightningStrikeObject)
public readonly struct StrikeData
{
    public readonly SkillStats Stats;
    public readonly Transform Target; // 대미지를 받을 적

    public StrikeData(SkillStats stats, Transform target)
    {
        Stats = stats;
        Target = target;
    }
}

// 스킬 오브젝트가 적을 때렸을 때 소환되는 오브젝트(SkillObjectBase.InitFromHit)가 받는 정보
// 소환된 오브젝트가 자기 초기화 데이터(ProjectileData, LinkData 등)를 이 정보로 직접 만듦
public readonly struct HitSpawnData
{
    public readonly SkillStats Stats;    // 맞힌 공격의 수치
    public readonly Vector2 Position;    // 맞은 적의 위치
    public readonly Vector2 Direction;   // 때린 오브젝트에서 적으로 향하는 방향 (겹쳐 있으면 오른쪽)
    public readonly Transform Owner;     // 스킬 사용자 (플레이어)
    public readonly Transform Target;    // 맞은 적

    public HitSpawnData(SkillStats stats, Vector2 position, Vector2 direction, Transform owner, Transform target)
    {
        Stats = stats;
        Position = position;
        Direction = direction;
        Owner = owner;
        Target = target;
    }
}
