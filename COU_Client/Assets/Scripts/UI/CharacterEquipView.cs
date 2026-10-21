using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CharacterEquipView : MonoBehaviour
{
    [Serializable]
    private class SlotUI
    {
        public EquipSlotType slotType;
        public Image frame;  
        public Image icon; 
        public Button button;
        public GameObject levelBg;   // 레벨 배지 배경
        public TMP_Text levelText;   // 장착한 장비의 "Lv1" 표시

        [NonSerialized] public Sprite defaultFrameSprite;
        [NonSerialized] public bool cached;
        [NonSerialized] public OwnedItem shownItem;
    }

    [SerializeField] private SlotUI[] slots;
    [SerializeField] private TMP_Text atkValueText;
    [SerializeField] private TMP_Text hpValueText;

    // 장비를 장착하지 않았을 때 띄우는 빈 슬롯 이미지.
    // 부위별 이미지(Empty + 부위명, 예: EmptyArmor)가 있으면 그걸 쓰고, 없으면 공통 기본 아이콘을 쓴다.
    private const string EmptySlotIconPath = "Equip/Inven/Item/Weapon/Weapon";
    private const string EmptySlotFolder = "Equip/Inven/Item/Weapon/EmptySlot/";
    private Sprite emptySlotSprite;
    private readonly System.Collections.Generic.Dictionary<EquipSlotType, Sprite> emptySlotSprites = new();

    // 아이콘 크기
    [SerializeField] private Vector2 equippedIconSize = new(100f, 100f);
    // 등급 프레임 좌상단 엠블럼을 피해 장착 아이콘을 오른쪽 아래로 옮긴다. 빈 슬롯은 가운데 그대로 둔다.
    [SerializeField] private Vector2 equippedIconOffset = new(4f, -8f);
    [SerializeField] private Vector2 emptySlotIconSize = new(156f, 156f);
    // 부위별 빈 슬롯 이미지는 프레임까지 포함돼 있어서 등급 프레임과 같은 크기로 띄운다.
    [SerializeField] private Vector2 emptySlotFramedSize = new(168f, 168f);

    private static readonly System.Collections.Generic.Dictionary<EquipSlotType, Vector2> SlotGridPosition = new()
    {
        { EquipSlotType.Weapon,   new Vector2(-380f, 240f) },
        { EquipSlotType.Armor,    new Vector2(380f, 240f) },
        { EquipSlotType.Necklace, new Vector2(-380f, 30f) },
        { EquipSlotType.Belt,     new Vector2(380f, 30f) },
        { EquipSlotType.Gloves,   new Vector2(-380f, -180f) },
        { EquipSlotType.Shoes,    new Vector2(380f, -180f) },
    };

    private void Awake()
    {
        emptySlotSprite = Resources.Load<Sprite>(EmptySlotIconPath);

        foreach (var slot in slots)
        {
            var perSlot = Resources.Load<Sprite>(EmptySlotFolder + "Empty" + slot.slotType);
            if (perSlot != null)
                emptySlotSprites[slot.slotType] = perSlot;

            if (slot.button != null && SlotGridPosition.TryGetValue(slot.slotType, out var pos))
            {
                var slotRect = slot.button.GetComponent<RectTransform>();
                if (slotRect != null)
                    slotRect.anchoredPosition = pos;
            }

            if (slot.button != null && slot.button.targetGraphic is Image bg)
            {
                var c = bg.color;
                c.a = 0f;
                bg.color = c;
            }

            if (slot.icon != null)
                slot.icon.color = Color.white;

            if (slot.button == null) continue;

            var captured = slot; 
            slot.button.onClick.AddListener(() => OnSlotClicked(captured));
        }
    }

    private void Start()
    {
        if (PlayerInventory.Instance == null) return;

        PlayerInventory.Instance.OnInventoryChanged += Refresh;
        GameManager.PlayerData.OnPlayerDataChanged += OnPlayerDataChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;
        if (PlayerDataManager.HasInstance)
            GameManager.PlayerData.OnPlayerDataChanged -= OnPlayerDataChanged;
    }

    private void OnSlotClicked(SlotUI slot)
    {
        var equippedItem = PlayerInventory.Instance.GetEquipped(slot.slotType);
        if (equippedItem == null) return; 

        UIManager.Instance.Get<EquipDetailPopUp>().Show(equippedItem);
    }

    private void OnPlayerDataChanged(PlayerSaveData _) => Refresh();

    private void Refresh()
    {
        foreach (var slot in slots)
            RefreshSlot(slot);

        var stat = PlayerStatCalculator.CalculateCurrent();
        if (atkValueText != null)
            SetValue(atkValueText, stat.FinalAttack.ToString());
        if (hpValueText != null)
            SetValue(hpValueText, stat.FinalHp.ToString());
    }

    // 값이 바뀌었을 때만 튕긴다
    private static void SetValue(TMP_Text text, string value)
    {
        if (text.text == value) return;

        text.text = value;
        UITween.Punch(text.transform);
    }

    private void RefreshSlot(SlotUI slot)
    {
        var firstRefresh = !slot.cached;
        if (!slot.cached)
        {
            slot.defaultFrameSprite = slot.frame != null ? slot.frame.sprite : null;
            slot.cached = true;
        }

        var equippedItem = PlayerInventory.Instance.GetEquipped(slot.slotType);

        // 장비를 바꿔 끼면 아이콘이 튀어나온다 (처음 그릴 때는 그대로)
        if (!firstRefresh && slot.shownItem != equippedItem && slot.icon != null)
            UITween.PopIn(slot.icon.transform, 0f, 0.5f);
        slot.shownItem = equippedItem;

        if (equippedItem != null)
        {
            var data = equippedItem.Data;

            if (slot.icon != null)
            {
                slot.icon.sprite = Resources.Load<Sprite>(data.iconPath);
                slot.icon.enabled = slot.icon.sprite != null;
                slot.icon.rectTransform.sizeDelta = equippedIconSize;
                slot.icon.rectTransform.anchoredPosition = equippedIconOffset;
            }

            if (slot.frame != null)
            {
                var gradeSprite = ItemDatabase.GetGradeIcon(slot.slotType, equippedItem.Grade);
                slot.frame.sprite = gradeSprite != null ? gradeSprite : slot.defaultFrameSprite;
                slot.frame.enabled = slot.frame.sprite != null;
            }

            SetLevel(slot, equippedItem.level);
        }
        else
        {

            if (slot.icon != null)
            {
                var hasPerSlot = emptySlotSprites.TryGetValue(slot.slotType, out var sprite);
                if (!hasPerSlot) sprite = emptySlotSprite;

                slot.icon.sprite = sprite;
                slot.icon.enabled = sprite != null;
                slot.icon.rectTransform.sizeDelta = hasPerSlot ? emptySlotFramedSize : emptySlotIconSize;
                slot.icon.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (slot.frame != null)
            {
                slot.frame.sprite = slot.defaultFrameSprite;
                slot.frame.enabled = slot.frame.sprite != null;
            }

            SetLevel(slot, null);
        }
    }

    // 장착 중이면 레벨을 보여 주고, 빈 슬롯이면 배지를 숨긴다
    private static void SetLevel(SlotUI slot, int? level)
    {
        var show = level.HasValue;

        if (slot.levelBg != null)
            slot.levelBg.SetActive(show);

        if (slot.levelText != null)
        {
            slot.levelText.gameObject.SetActive(show);
            if (show)
                slot.levelText.text = $"Lv{level.Value}";
        }
    }
}
