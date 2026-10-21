using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotView : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image gradeIcon;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text levelText;
    
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject checkOverlay;

    private OwnedItem boundItem;
    private SynthesisWindow synthesisTarget;

    private void Awake()
    {
        button.onClick.AddListener(OnClicked);
    }

    public void Setup(OwnedItem item, SynthesisWindow synthesisTarget = null)
    {
        boundItem = item;
        this.synthesisTarget = synthesisTarget;
        var data = item.Data;

        itemIcon.sprite = Resources.Load<Sprite>(data.iconPath);
        itemIcon.enabled = itemIcon.sprite != null;

        gradeIcon.sprite = ItemDatabase.GetGradeIcon(data.SlotType, item.Grade);
        gradeIcon.enabled = gradeIcon.sprite != null;

        if (levelText != null)
            levelText.text = $"Lv{item.level}";

        RefreshSynthesisOverlay();
    }

    private void RefreshSynthesisOverlay()
    {
        if (lockOverlay == null && checkOverlay == null)
            return;

        if (synthesisTarget == null)
        {
            if (lockOverlay != null) lockOverlay.SetActive(false);
            if (checkOverlay != null) checkOverlay.SetActive(false);
            return;
        }

        var bound = synthesisTarget.BoundItem;
        // 합성 재료/대상 기준(서버 findMaterials와 동일): 같은 아이템 + 같은 등급, 재료는 미장착만
        var isMatch = bound != null
            && bound.CanSynthesize
            && bound.itemId == boundItem.itemId
            && bound.Grade == boundItem.Grade
            && (boundItem == bound || !boundItem.isEquipped);

        // 오버레이 ㅈ게ㅓ
        if (lockOverlay != null) lockOverlay.SetActive(false);
        if (checkOverlay != null)
        {
            var wasShown = checkOverlay.activeSelf;
            checkOverlay.SetActive(isMatch);
            if (isMatch && !wasShown)
                UITween.PopIn(checkOverlay.transform, 0f, 0f);   // 합성 재료로 맞게 되면 체크가 튀어나온다
        }
    }

    private void OnClicked()
    {
        if (synthesisTarget != null)
            synthesisTarget.Show(boundItem);
        else
            UIManager.Instance.Get<EquipDetailPopUp>().Show(boundItem);
    }
}
