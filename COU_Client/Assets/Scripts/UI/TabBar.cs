using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabBar : UIView
{
    [Serializable]
    private class Tab
    {
        public Button button;
        public Image background;
        public LayoutElement layout;
        public RectTransform icon;
        public GameObject label;
        public GameObject indicator;
        public GameObject badge;
        public TMP_Text badgeText;
        public UIView screen;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private int startIndex = 2;
    [SerializeField] private UIColor selectedColor = UIColor.Surface;
    [SerializeField] private UIColor normalColor = UIColor.Base;
    [SerializeField] private float selectedWidth = 1.9f;
    [SerializeField] private float selectedIconScale = 1.33f;

    private UIManager manager;
    private int selectedIndex = -1;   // -1: 아직 선택 전. 첫 선택은 연출 없이 바로 적용한다

    private void Awake()
    {
        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            tabs[i].button.onClick.AddListener(() => Select(index));
        }

        manager = UIManager.Instance;
        manager.ThemeChanged += ApplyColors;
    }

    private void Start()
        => Select(startIndex);

    private void OnDestroy()
    {
        if (manager != null)
            manager.ThemeChanged -= ApplyColors;
    }

    public void Select(int index)
    {
        var screen = tabs[index].screen;
        if (screen.Layer != UILayer.HUD)
        {
            screen.Open();
            return;
        }

        var animate = selectedIndex >= 0;
        selectedIndex = index;
        ApplyColors();

        for (var i = 0; i < tabs.Length; i++)
        {
            var tab = tabs[i];
            var selected = i == index;
            var width = selected ? selectedWidth : 1f;
            var iconScale = Vector3.one * (selected ? selectedIconScale : 1f);

            tab.layout.DOKill();
            tab.icon.DOKill();
            if (animate)
            {
                // 선택한 탭이 넓어지고 아이콘이 커진다
                DOTween.To(() => tab.layout.flexibleWidth, value => tab.layout.flexibleWidth = value, width, 0.2f)
                    .SetEase(Ease.OutCubic).SetTarget(tab.layout).AsUI(tab.layout);
                tab.icon.DOScale(iconScale, 0.2f).SetEase(Ease.OutBack).AsUI(tab.icon);
            }
            else
            {
                tab.layout.flexibleWidth = width;
                tab.icon.localScale = iconScale;
            }

            var wasLabelShown = tab.label.activeSelf;
            tab.label.SetActive(selected);
            tab.indicator.SetActive(selected);
            if (animate && selected && !wasLabelShown)
                UITween.PopIn(tab.label.transform, 0.05f, 0f);

            if (selected)
                tab.screen.Open();
            else
                tab.screen.Close();
        }
    }

    private void ApplyColors()
    {
        for (var i = 0; i < tabs.Length; i++)
            tabs[i].background.color = UIPalette.Get(i == selectedIndex ? selectedColor : normalColor);
    }

    public void SetBadge(int index, string mark)
    {
        var badge = tabs[index].badge;
        var show = !string.IsNullOrEmpty(mark);
        var wasShown = badge.activeSelf;

        badge.SetActive(show);
        tabs[index].badgeText.text = mark;

        if (show && !wasShown)
            UITween.PopIn(badge.transform, 0f, 0f);
    }
}
