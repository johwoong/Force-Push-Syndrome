using UnityEngine;

// 피해를 주지 않는 이펙트 전용 스킬 오브젝트. 소환될 때 효과음(_sfx)이 재생되고, _lifeTime(애니메이션 길이)이 지나면 풀에 스스로 반환됨
// 공격 스킬과 같은 AoEData로 초기화되므로 SkillAttackObject의 타격 시 소환 프리팹 칸에 공격 스킬과 똑같이 지정할 수 있음
// 타격음, 넉백처럼 공격에만 필요한 설정은 없음
public sealed class SkillEffectObject : SkillObject<AoEData>
{
    // 풀에 반환되기까지의 시간 (이펙트 애니메이션 길이에 맞춤)
    [SerializeField] private float _lifeTime = 0.5f;

    private float _elapsed;

    public override bool CanSpawnOnHit => true;

    public override void InitFromHit(HitSpawnData hit)
    {
        Init(new AoEData(hit.Stats, hit.Position));
    }

    protected override void OnInit(AoEData data)
    {
        stats = data.Stats;
        OnLaunch();
    }

    protected override void OnLaunch()
    {
        _elapsed = 0.0f;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if(_elapsed >= _lifeTime)
        {
            ReturnToPool();
        }
    }
}
