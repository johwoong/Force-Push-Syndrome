using UnityEngine;

// 지정된 타겟을 쫓아가는 탄환. 타겟이 사라지면 마지막 방향으로 직진
// 추적, 관통, 연쇄 처리는 LinearProjectile이 담당하고 여기서는 처음 쫓을 적만 정함
// 마지막으로 적을 맞히면 그 자리에 멈춰 _hitFlipbook을 재생한 뒤 풀에 반환 (비워두면 바로 반환)
public sealed class HomingProjectile : LinearProjectile
{
    [SerializeField] private SpriteRenderer _renderer;

    // 맞힌 뒤 재생할 애니메이션. 프리팹에서는 꺼 두고 맞혔을 때 켬
    [SerializeField] private SpriteFlipbook _hitFlipbook;

    // 맞힌 뒤 풀에 반환하기까지의 시간. _hitFlipbook의 _duration과 같은 값을 입력
    [SerializeField] private float _hitDisplayTime = 0.4f;

    // 날아가는 동안 보여줄 스프라이트 (프리팹에 지정된 스프라이트). 애니메이션이 바꿔 둔 스프라이트를 다음 발사 때 되돌리기 위해 저장
    private Sprite _flightSprite;
    private bool _isPlayingHit;
    private float _hitElapsed;

    private void Awake()
    {
        if(_renderer != null)
        {
            _flightSprite = _renderer.sprite;
        }
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        _isPlayingHit = false;

        if(_hitFlipbook != null)
        {
            _hitFlipbook.enabled = false;
        }

        if(_renderer != null)
        {
            _renderer.sprite = _flightSprite;
        }

        if(target != null && target.TryGetComponent(out Enemy targetEnemy))
        {
            homingTarget = targetEnemy;
        }
    }

    protected override void Movement()
    {
        if(!_isPlayingHit)
        {
            base.Movement();
            return;
        }

        // 애니메이션 재생 중에는 제자리에 멈춤
        _hitElapsed += Time.deltaTime;

        if(_hitElapsed >= _hitDisplayTime)
        {
            ReturnToPool();
        }
    }

    protected override void OnFinishHit()
    {
        if(_hitFlipbook == null)
        {
            base.OnFinishHit();
            return;
        }

        _isPlayingHit = true;
        _hitElapsed = 0f;

        // 애니메이션 도중 수명이 다해 끊기지 않도록 수명을 다시 셈
        elapsed = 0f;

        _hitFlipbook.enabled = true;
    }
}
