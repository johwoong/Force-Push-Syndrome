using UnityEngine;

// 모든 스킬 오브젝트의 비제네릭 부모. 종류와 상관없이 하나의 타입으로 다뤄야 할 때 사용 (SkillPrefabTable, 타격 시 소환 프리팹 등)
// 공격 오브젝트(SkillAttackObject)와 이펙트 전용 오브젝트(SkillEffectObject)가 공통으로 가진 것만 둠: 소환될 때 재생되는 효과음
public abstract class SkillObjectBase : MonoBehaviour
{
    // 프리팹마다 인스펙터에서 지정하는 효과음. 비워 두면 PlaySfx를 불러도 소리가 나지 않음
    [SerializeField] private AudioClip _sfx;

    // 지정된 효과음을 SoundManager로 재생. 소리를 낼 순간에 서브클래스가 부름
    protected void PlaySfx(float volumeScale = 1f, float pitch = 1f)
    {
        if(_sfx == null)
        {
            return;
        }

        GameManager.Sound.PlaySFX(_sfx, volumeScale, pitch);
    }

    // 다른 스킬 오브젝트가 적을 때렸을 때 이 오브젝트가 소환될 수 있는지. 소환할 수 있는 오브젝트만 true로 오버라이드함
    // 스킬이 직접 해제해야 사라지는 지속형(스피커, 지속 범위)은 타격 때마다 소환되면 쌓이기만 하므로 false로 둠
    public virtual bool CanSpawnOnHit => false;

    // 타격 시 소환된 오브젝트가 맞힌 정보(HitSpawnData)로 자기 초기화 데이터를 만들어 Init을 부르는 곳. CanSpawnOnHit가 true인 오브젝트가 구현
    public virtual void InitFromHit(HitSpawnData hit)
    {
    }
}
