using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LuckTrainRewardCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject[] filledStars;
    [SerializeField] private TMP_Text descriptionText;

    public void Set(Sprite icon, string name, int grade, string description)
    {
        iconImage.sprite = icon;
        nameText.text = name;
        for (var i = 0; i < filledStars.Length; i++)
            filledStars[i].SetActive(i < grade);
        descriptionText.text = description;
    }
}
