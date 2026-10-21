using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 누르면 살짝 줄었다가 놓으면 돌아온다. pulse를 켜면 평소에 숨 쉬듯 커졌다 작아진다
[RequireComponent(typeof(Button))]
public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private bool pulse;

    private Button button;
    private Vector3 baseScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (pulse)
            UITween.Pulse(transform);
    }

    // 꺼질 때 끊긴 트윈의 중간 크기가 남지 않게 원래 크기로 되돌린다
    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.IsActive() || !button.IsInteractable()) return;

        transform.DOKill();
        transform.DOScale(baseScale * pressedScale, 0.08f).AsUI(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        var release = transform.DOScale(baseScale, 0.15f).SetEase(Ease.OutBack).AsUI(this);
        if (pulse)
            release.OnComplete(() => UITween.Pulse(transform));
    }
}
