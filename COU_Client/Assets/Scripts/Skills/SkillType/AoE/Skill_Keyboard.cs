using UnityEngine;

// 기본: 쿨타임마다 사용자 위치에서 가장 가까운 적 방향으로 휘두름 (범위의 위치/크기는 프리팹 콜라이더가 결정)
// 진화: 쿨타임마다 휘두르지 않고, 바라보는 방향에 지속 범위(FacingFieldObject)를 두고 틱마다 피해를 줌
//       틱 간격 = 최종 쿨타임 ÷ (1 + 반복 횟수), 틱 대미지 = 한 번 휘두를 때의 대미지 그대로 (대미지 보너스 포함)
//       반복은 따로 발동하지 않고 틱 간격에 반영됨
//       진화 프리팹은 SkillPrefabTable의 EvolvedPrefab에 FacingFieldObject가 붙은 프리팹을 지정해야 함
public sealed class Skill_Keyboard : AoeSkillBase
{
    // 틱 간격의 하한 (쿨타임 감소와 반복이 많이 쌓여도 한 프레임에 판정이 몰리지 않도록)
    private const float MinFieldTickInterval = 0.05f;

    private FacingFieldObject _field;
    private float _fieldTickTimer;

    // 진화 프리팹에 FacingFieldObject가 없어 지속 범위를 만들 수 없는 경우 (매 프레임 다시 시도하지 않도록 기억)
    private bool _isFieldUnavailable;

    public override void Tick(float deltaTime)
    {
        // 지속 범위를 만들 수 없으면 기본 휘두르기로 동작
        if(!IsEvolved || !TickField(deltaTime))
        {
            base.Tick(deltaTime);
        }
    }

    protected override void Fire()
    {
        // 사용자 위치에서 가장 가까운 적 방향으로 휘두름. 범위의 위치/크기는 프리팹 콜라이더가 결정
        Spawn(transform.position, new AoEData(BuildStats(), transform.position, fireDirection));
    }

    // 진화하자마자 첫 틱이 나가도록 함 (지속 범위는 다음 Tick에서 만듦)
    protected override void OnEvolved()
    {
        _fieldTickTimer = 0.0f;
    }

    protected override void OnUnequip()
    {
        base.OnUnequip();
        DespawnField();
    }

    // 지속 범위를 유지하고 틱마다 판정함. 지속 범위를 만들 수 없으면 false
    private bool TickField(float deltaTime)
    {
        if(!EnsureField())
        {
            return false;
        }

        _fieldTickTimer -= deltaTime;

        if(_fieldTickTimer > 0.0f)
        {
            return true;
        }

        // 쿨타임 감소, 반복, 대미지 보너스가 플레이 중에 바뀔 수 있으므로 틱마다 현재 값으로 다시 계산
        _fieldTickTimer = GetFieldTickInterval();
        _field.Hit(BuildStats(), _fieldTickTimer);

        return true;
    }

    private float GetFieldTickInterval()
    {
        int repeatCount = GetFlatStat(SkillFlatStat.Repeat, 0);

        return Mathf.Max(GetCooldown() / (1 + repeatCount), MinFieldTickInterval);
    }

    // 지속 범위가 없으면 진화 프리팹으로 만듦 (SkillBase.Spawn이 진화한 스킬은 EvolvedPrefab을 소환함)
    private bool EnsureField()
    {
        if(_isFieldUnavailable)
        {
            return false;
        }

        if(_field != null && _field.gameObject.activeInHierarchy)
        {
            return true;
        }

        GameObject instance = Spawn(transform.position, new FacingFieldData(BuildStats(), transform, movement));

        if(instance == null)
        {
            _isFieldUnavailable = true;
            return false;
        }

        if(!instance.TryGetComponent(out _field))
        {
            Debug.LogWarning($"[Skill_Keyboard] 진화 프리팹(SkillPrefabTable의 EvolvedPrefab)이 비어 있거나 FacingFieldObject가 없어 기본 휘두르기로 동작합니다: {SkillId}");

            SkillObjectPool.Instance.Return(instance);
            _isFieldUnavailable = true;
            return false;
        }

        return true;
    }

    private void DespawnField()
    {
        if(_field != null && _field.gameObject.activeInHierarchy)
        {
            SkillObjectPool.Instance.Return(_field.gameObject);
        }

        _field = null;
    }
}
