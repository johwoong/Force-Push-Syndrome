using DG.Tweening;
using UnityEngine;

// 로그인 배경: 스테이지 배경들을 일정 주기로 크로스페이드하며 돌려 보여준다
// front(위)가 페이드 아웃되면서 아래에 미리 깔아 둔 back(다음 배경)이 드러난다
public class LoginBackgroundSlideshow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer front;      // 지금 배경
    [SerializeField] private SpriteRenderer back;       // 다음 배경 (front 아래)
    [SerializeField] private Sprite[] backgrounds;      // 스테이지 1~5 배경
    [SerializeField] private float interval = 10f;      // 배경이 바뀌는 주기(초, 페이드 포함)
    [SerializeField] private float fadeDuration = 2f;

    private int index;

    private void Start()
    {
        if (backgrounds.Length < 2) return;

        Show(0);

        DOTween.Sequence()
            .AppendInterval(interval - fadeDuration)
            .Append(front.DOFade(0f, fadeDuration))
            .AppendCallback(() => Show((index + 1) % backgrounds.Length))
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    // newIndex번 배경을 front에 띄우고, 그다음 배경을 back에 미리 깔아 둔다
    private void Show(int newIndex)
    {
        index = newIndex;
        front.sprite = backgrounds[index];
        front.color = Color.white;   // 페이드로 내려간 알파 복구
        back.sprite = backgrounds[(index + 1) % backgrounds.Length];
    }
}
