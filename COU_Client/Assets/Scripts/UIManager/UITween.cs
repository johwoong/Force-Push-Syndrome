using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI 연출 공통. 전투 중 창은 timeScale 0에서 열리므로 UI 트윈은 모두 unscaled로 돌린다
public static class UITween
{
    private const int MaxStaggerSteps = 12;   // 목록이 길어도 마지막 항목이 너무 늦게 나오지 않게

    // unscaled로 돌고, owner가 파괴되면 같이 정리된다
    public static T AsUI<T>(this T tween, Component owner) where T : Tween
        => tween.SetUpdate(true).SetLink(owner.gameObject);

    public static Tween FadeIn(CanvasGroup group, float duration = 0.2f)
    {
        group.DOKill();
        group.alpha = 0f;
        return group.DOFade(1f, duration).AsUI(group);
    }

    // 원래 크기의 from배에서 튀어나오듯 커진다. from이 0이면 delay 동안 보이지 않는다
    public static Tween PopIn(Transform target, float delay = 0f, float from = 0.8f, float duration = 0.25f)
    {
        target.DOKill(true);
        var scale = target.localScale;
        target.localScale = scale * from;
        return target.DOScale(scale, duration).SetDelay(delay).SetEase(Ease.OutBack).AsUI(target);
    }

    // 켜져 있는 항목만 차례로 튀어나온다
    public static void Stagger<T>(IEnumerable<T> items, float interval = 0.04f) where T : Component
    {
        var step = 0;
        foreach (var item in items)
        {
            if (item.gameObject.activeSelf)
                PopIn(item.transform, Mathf.Min(step++, MaxStaggerSteps) * interval, 0f);
        }
    }

    // 현재 크기 기준으로 한 번 튕긴다
    public static Tween Punch(Transform target, float strength = 0.15f, float delay = 0f)
    {
        target.DOKill(true);
        return target.DOPunchScale(target.localScale * strength, 0.25f, 6, 0.5f).SetDelay(delay).AsUI(target);
    }

    // offset만큼 떨어진 곳에서 원래 자리로 들어온다
    public static Tween SlideIn(RectTransform target, Vector2 offset, float duration = 0.35f)
    {
        target.DOKill(true);
        var home = target.anchoredPosition;
        target.anchoredPosition = home + offset;
        return target.DOAnchorPos(home, duration).SetEase(Ease.OutBack).AsUI(target);
    }

    // 숨 쉬듯 커졌다 작아지기를 반복한다. 꺼져 있는 동안은 멈춘다
    public static void Pulse(Transform target, float strength = 0.08f, float duration = 0.6f)
    {
        StopPulse(target);
        target.DOScale(target.localScale * (1f + strength), duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(target.gameObject, LinkBehaviour.PauseOnDisablePlayOnEnable);
    }

    // 반복을 멈추고 시작 크기로 되돌린다
    public static void StopPulse(Transform target)
    {
        target.DORewind();
        target.DOKill();
    }

    // 알파를 낮췄다 올리기를 반복한다
    public static void Blink(Graphic target, float minAlpha = 0.3f, float duration = 0.5f)
    {
        StopBlink(target);
        target.DOFade(minAlpha, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(target.gameObject, LinkBehaviour.PauseOnDisablePlayOnEnable);
    }

    // 반복을 멈추고 시작 알파로 되돌린다
    public static void StopBlink(Graphic target)
    {
        target.DORewind();
        target.DOKill();
    }

    // fillAmount를 부드럽게 바꾼다.
    // wrapWhenLower면 값이 줄어든 것을 한 바퀴 돈 것(레벨업)으로 보고 끝까지 채운 뒤 0부터 다시 채운다
    public static Tween Fill(Image image, float ratio, bool wrapWhenLower = false, float duration = 0.3f)
    {
        image.DOKill(true);
        if (!wrapWhenLower || ratio >= image.fillAmount)
            return image.DOFillAmount(ratio, duration).SetEase(Ease.OutCubic).AsUI(image);

        return DOTween.Sequence()
            .Append(image.DOFillAmount(1f, duration * 0.5f))
            .AppendCallback(() => image.fillAmount = 0f)
            .Append(image.DOFillAmount(ratio, duration * 0.5f))
            .SetTarget(image)
            .AsUI(image);
    }

    // 숫자를 from에서 to까지 굴려서 보여준다. from이 음수(처음 표시)거나 to와 같으면 바로 표시한다
    public static Tween Count(TMP_Text text, int from, int to, Func<int, string> format, float duration = 0.5f)
    {
        text.DOKill();
        if (from < 0 || from == to)
        {
            text.text = format(to);
            return null;
        }

        text.text = format(from);   // 첫 갱신 전 한 프레임 동안 이전 글자가 남지 않게
        return DOTween.To(() => from, value => text.text = format(value), to, duration)
            .SetEase(Ease.OutCubic)
            .SetTarget(text)
            .AsUI(text);
    }

    public static Tween ScrollTo(ScrollRect scroll, float verticalNormalized, float duration = 0.3f)
    {
        scroll.DOKill();
        scroll.StopMovement();
        return scroll.DOVerticalNormalizedPos(verticalNormalized, duration).SetEase(Ease.OutCubic).AsUI(scroll);
    }
}
