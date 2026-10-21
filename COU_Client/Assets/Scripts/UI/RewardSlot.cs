using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardSlot : MonoBehaviour
{
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    public void Set(ChallengeReward reward)
    {
        frame.color = GradeColor.Get(reward.grade);
        icon.color = reward.iconColor;
        amountText.text = $"x {reward.amount}";
    }

    public void Set(Sprite iconSprite, int amount)
    {
        frame.color = GradeColor.Get(ItemGrade.General);
        icon.sprite = iconSprite;
        icon.color = Color.white;
        amountText.text = $"x {amount:N0}";
    }
}
