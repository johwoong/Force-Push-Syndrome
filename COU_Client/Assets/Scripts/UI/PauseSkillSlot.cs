using UnityEngine;
using UnityEngine.UI;

public class PauseSkillSlot : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject[] filledStars;

    public void Set(Sprite icon, int grade)
    {
        content.SetActive(true);
        iconImage.sprite = icon;
        for (var i = 0; i < filledStars.Length; i++)
            filledStars[i].SetActive(i < grade);
    }

    public void Clear()
        => content.SetActive(false);
}
