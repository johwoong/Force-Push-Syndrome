using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Vibration")]
    [SerializeField] private long vibrateMs = 40;          // 짧고 가볍게 (30~60ms 권장)

    [Header("Border Flash")]
    [SerializeField] private Image borderImage;            // 화면 가장자리 빨간 테두리
    [SerializeField] private float flashAlpha = 0.45f;
    [SerializeField] private float flashTime = 0.08f;      // 켜지는 시간(꺼지는 시간도 같음)

    [Header("Throttle")]
    [SerializeField] private float cooldown = 0.25f;       // 몬스터 여러 마리에게 연속으로 맞을 때 과도한 연출 방지

    private float nextPlayTime;

    private void Awake()
    {
        if (playerHealth == null) { playerHealth = GetComponent<PlayerHealth>(); }
        if (borderImage != null)
        {
            SetBorderAlpha(0f);
            borderImage.raycastTarget = false;             // 화면을 덮어도 터치(조이스틱)를 막지 않게
        }
        else
        {
            Debug.LogWarning("[HitFeedback] borderImage가 비어 있습니다. 씬의 Player에 HitBorder를 연결하세요.", this);
        }
    }

    private void OnEnable() 
    {
        if (playerHealth != null) { playerHealth.OnDamaged += Play; }
    }
    private void OnDisable() 
    {
        if (playerHealth != null) { playerHealth.OnDamaged -= Play; }
    }

    private void Play()
    {
        if (Time.time < nextPlayTime) return;
        nextPlayTime = Time.time + cooldown;

        Haptics.Vibrate(vibrateMs);
        Flash();
        GameManager.Sound.PlaySFX(GameConstants.Sfx.PLAYER_HIT, 0.8f, Random.Range(0.95f, 1.05f));
    }

    private void Flash()
    {
        if (borderImage == null) { return; }

        borderImage.DOKill();                // 진행 중인 깜빡임 정리
        SetBorderAlpha(0f);
        // 0 -> flashAlpha -> 0 : Yoyo 루프 2번(갔다가 돌아옴)
        borderImage.DOFade(flashAlpha, flashTime)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void SetBorderAlpha(float alpha)
    {
        Color c = borderImage.color;
        c.a = alpha;
        borderImage.color = c;
    }

    private void OnDestroy()
    {
        if (borderImage != null) borderImage.DOKill();
    }
}
