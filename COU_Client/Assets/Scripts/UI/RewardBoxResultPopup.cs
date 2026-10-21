using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardBoxResultPopup : UIPopup
{
    [SerializeField] private TMP_Text boxCountText;
    [SerializeField] private Transform content;
    [SerializeField] private ItemSlotView itemSlotPrefab;
    [SerializeField] private Button confirmButton;

    private readonly List<ItemSlotView> slots = new();

    private void Awake()
    {
        confirmButton.onClick.AddListener(Close);
    }

    public void Show(IReadOnlyList<OwnedItem> items)
    {
        boxCountText.text = $"x{items.Count}";

        while (slots.Count < items.Count)
            slots.Add(Instantiate(itemSlotPrefab, content));

        for (var i = 0; i < slots.Count; i++)
        {
            var active = i < items.Count;
            slots[i].gameObject.SetActive(active);
            if (active)
                slots[i].Setup(items[i]);
        }

        Open();
        UITween.Punch(boxCountText.transform, 0.3f);
        UITween.Stagger(slots, 0.06f);
    }
}
