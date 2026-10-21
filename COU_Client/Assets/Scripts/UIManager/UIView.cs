using UnityEngine;

public abstract class UIView : MonoBehaviour
{
    [SerializeField] private RectTransform panel;   // 열릴 때 튀어나오는 본체. 비워 두면 페이드만 한다

    private CanvasGroup canvasGroup;

    public UILayer Layer { get; internal set; }

    public bool IsOpen => gameObject.activeSelf;

    // 닫혀 있다가 열릴 때만 연출한다. 이미 열린 창을 다시 Open하면 그대로 둔다
    public virtual void Open()
    {
        if (IsOpen) return;

        gameObject.SetActive(true);

        if (canvasGroup == null && !TryGetComponent(out canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        UITween.FadeIn(canvasGroup);

        if (panel != null)
            UITween.PopIn(panel, 0f, 0.9f);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
