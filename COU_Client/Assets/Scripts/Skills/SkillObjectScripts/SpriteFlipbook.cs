using UnityEngine;

// 지정한 시간(_duration) 동안 스프라이트들을 같은 간격으로 한 번씩 보여주는 단순 애니메이션. 풀에서 켜질 때마다 처음부터 재생
// 반복이 꺼져 있으면 마지막 스프라이트가 계속 유지되고, 켜져 있으면 처음부터 다시 반복
// 풀 반환은 하지 않음. 반환 시간(AoeHitObject._lifeTime, LightningObject._displayTime 등)과 같은 값을 _duration에 입력
public sealed class SpriteFlipbook : MonoBehaviour
{
    // 재생 시간이 0 이하일 때 나누기 오류를 막기 위한 최소값
    private const float MinDuration = 0.001f;

    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;

    // 모든 스프라이트를 한 번 보여주는 데 걸리는 시간(초). 스프라이트 수로 균등하게 나눠 보여줌
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private bool _loop;

    private float _elapsed;

    private void OnEnable()
    {
        if(_renderer == null && !TryGetComponent(out _renderer))
        {
            Debug.LogWarning($"[SpriteFlipbook] SpriteRenderer가 없습니다: {name}");
            enabled = false;
            return;
        }

        _elapsed = 0.0f;
        ApplyFrame();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if(_frames == null || _frames.Length == 0)
        {
            return;
        }

        int index = (int)(_elapsed / Mathf.Max(_duration, MinDuration) * _frames.Length);

        index = _loop ? index % _frames.Length : Mathf.Min(index, _frames.Length - 1);

        _renderer.sprite = _frames[index];
    }
}
