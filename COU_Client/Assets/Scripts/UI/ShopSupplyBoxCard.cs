using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지원품 상자 카드(ShopContent.prefab의 SupplyBox_*) 하나에 붙는 컴포넌트.
public class ShopSupplyBoxCard : MonoBehaviour
{
    [SerializeField] private Button actionButton;
    [SerializeField] private int productId;  
    [SerializeField] private ItemGrade minGrade; 
    [SerializeField] private ItemGrade maxGrade;
    [SerializeField] private int gemCost;   

    [SerializeField] private Color affordableColor = new Color(0.243f, 0.702f, 0.008f, 1f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color affordableTextColor = new Color32(0x28, 0x31, 0x3C, 0xFF);
    [SerializeField] private Color normalTextColor = Color.white;

    private void Awake()
    {
        actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void OnEnable()
    {
        PlayerInventory.Instance.OnInventoryChanged += UpdateAffordability;
        UpdateAffordability();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= UpdateAffordability;
    }

    private void UpdateAffordability()
    {
        var affordable = PlayerInventory.Instance.Gem >= gemCost;
        actionButton.targetGraphic.color = affordable ? affordableColor : normalColor;
        label.color = affordable ? affordableTextColor : normalTextColor;
    }

    private void OnActionButtonClicked()
    {
        if (PlayerInventory.Instance.Gem < gemCost)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", "보석이 부족합니다.");
            return;
        }

        actionButton.interactable = false;   // 응답이 올 때까지 연타 방지
        GameManager.ServerShop.Purchase(productId, OnPurchased);
    }

    private void OnPurchased(bool success, string message, List<OwnedItem> rewardedItems)
    {
        if (this == null) return;   // 응답이 오기 전에 씬이 바뀐 경우

        actionButton.interactable = true;

        if (!success)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", message);
            return;
        }

        GameManager.Sound.PlaySFX(GameConstants.Sfx.BUY_SUPPLY_BOX);

        if (rewardedItems != null && rewardedItems.Count > 0)
            UIManager.Instance.Get<SupplyResultPopup>().Show(rewardedItems[0]);
    }
}
