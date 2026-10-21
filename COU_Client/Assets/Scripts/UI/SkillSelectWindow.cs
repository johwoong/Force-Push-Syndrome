using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectWindow : UIView
{
    [SerializeField] private SkillSelectCard[] cards;
    [SerializeField] private Image[] weaponSlotIcons;
    [SerializeField] private Image[] supportSlotIcons;

    public event Action<int> OnSelected;

    private void Awake()
    {
        for (var i = 0; i < cards.Length; i++)
        {
            var index = i;
            cards[i].OnClicked += () => OnSelected?.Invoke(index);
        }
    }

    public void SetOption(int index, Sprite icon, string title, string description, int grade, bool isNew, SkillCategory? category = null)
        => cards[index].Set(icon, title, description, grade, isNew, category);

    public void SetWeaponSlots(IReadOnlyList<Sprite> icons)
        => SetSlots(weaponSlotIcons, icons);

    public void SetSupportSlots(IReadOnlyList<Sprite> icons)
        => SetSlots(supportSlotIcons, icons);

    private static void SetSlots(Image[] slots, IReadOnlyList<Sprite> icons)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var filled = i < icons.Count;
            slots[i].gameObject.SetActive(filled);
            if (filled)
                slots[i].sprite = icons[i];
        }
    }

    // 연속 레벨업으로 열린 채 다시 불려도 카드가 새로 나온다
    public void Show(int count)
    {
        for (var i = 0; i < cards.Length; i++)
            cards[i].gameObject.SetActive(i < count);
        Open();
        UITween.Stagger(cards, 0.08f);
        GameManager.Sound.PlaySFX(GameConstants.Sfx.LEVEL_UP);
    }
}
