using UnityEngine;

// 오브젝트를 Z축으로 일정한 속도로 계속 회전시키는 단순 애니메이션. 풀에서 켜질 때마다 처음 각도에서 다시 시작
// 양수는 반시계, 음수는 시계 방향
// 스킬 오브젝트가 루트의 회전을 직접 정하는 경우(AoeHitObject의 방향 회전 등)가 있으므로 루트가 아니라 스프라이트가 있는 자식에 붙일 것
public sealed class SpinAnimation : MonoBehaviour
{
    // 초당 회전 각도 (360이면 1초에 한 바퀴)
    [SerializeField] private float _degreesPerSecond = 360.0f;

    private Quaternion _startRotation;
    private bool _initialized;

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때 돌아간 각도가 아니라 프리팹 원래 각도에서 시작하기 위해 첫 활성화에서 한 번만 저장
        if(!_initialized)
        {
            _initialized = true;
            _startRotation = transform.localRotation;
        }

        transform.localRotation = _startRotation;
    }

    private void Update()
    {
        transform.Rotate(0.0f, 0.0f, _degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
