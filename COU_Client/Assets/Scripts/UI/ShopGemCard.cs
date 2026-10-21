using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopGemCard : MonoBehaviour
{
    [SerializeField] private Button buyButton;
    [SerializeField] private int productId;  
    [SerializeField] private int gemAmount;   

    private void Awake()
    {
        buyButton.onClick.AddListener(OnCardClicked);
    }

    private int gemBefore;

    private void OnCardClicked()
    {
        if (PlayerInventory.Instance.Gem < gemAmount)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", "보석이 부족합니다.");
            return;
        }

        UIManager.Instance.Get<MessagePopup>().ShowConfirm(
            $"보석 결제는 구현되어 있지 않습니다.\n대신 돈 넣고 돈 먹기를 진행합니다.\n\n당첨: 보석 +{gemAmount:N0}\n꽝: 보석 -{gemAmount:N0}\n\n하시겠습니까?",
            Purchase, title: "돈 넣고 돈 먹기");
    }

    private void Purchase()
    {
        gemBefore = PlayerInventory.Instance.Gem;
        buyButton.interactable = false;   // 응답이 올 때까지 연타 방지
        GameManager.ServerShop.Purchase(productId, OnPurchased);
    }

    private void OnPurchased(bool success, string message, List<OwnedItem> rewardedItems)
    {
        if (this == null) return;   // 응답이 오기 전에 씬이 바뀐 경우

        buyButton.interactable = true;

        if (!success)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", message);
            return;
        }

        // 응답에 당첨 여부가 없어 서버가 덮어쓴 보유량으로 판단한다
        if (PlayerInventory.Instance.Gem > gemBefore)
        {
            GameManager.Sound.PlaySFX(GameConstants.Sfx.BUY_GEM);
            UIManager.Instance.Get<LogPopup>().Show("당첨!", $"보석 +{gemAmount:N0}");
        }
        else
        {
            UIManager.Instance.Get<LogPopup>().Show("꽝...", $"보석 -{gemAmount:N0}");
        }
    }
}
