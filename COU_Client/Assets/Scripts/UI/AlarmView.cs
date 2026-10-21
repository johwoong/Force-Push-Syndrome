using DG.Tweening;
using TMPro;
using UnityEngine;

// 화면 가운데 경고 띠. 위아래로 펼쳐지고 문구가 깜빡이다가 시간이 지나면 닫힌다
public class AlarmView : UIView
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float showSeconds = 2f;

    public void Show(string message)
    {
        messageText.text = message;
        Open();

        transform.DOKill();
        transform.localScale = new Vector3(1f, 0f, 1f);
        messageText.alpha = 1f;

        DOTween.Sequence()
            .Append(transform.DOScaleY(1f, 0.2f).SetEase(Ease.OutBack))
            .Append(messageText.DOFade(0.3f, 0.2f).SetLoops(4, LoopType.Yoyo))
            .AppendInterval(showSeconds)
            .AppendCallback(Close)
            .SetTarget(transform)
            .AsUI(this);
    }
}
