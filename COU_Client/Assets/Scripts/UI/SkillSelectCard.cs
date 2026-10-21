using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject newTag;
    [SerializeField] private Image categoryTagImage;   // 좌하단 "무기"/"지원" 배지
    [SerializeField] private TMP_Text categoryTagText;
    [SerializeField] private Color weaponTagColor = new(0.93f, 0.45f, 0.16f);
    [SerializeField] private Color supportTagColor = new(0.20f, 0.60f, 0.92f);
    [SerializeField] private TMP_Text[] stars;
    [SerializeField] private Color filledColor = new(1f, 0.776f, 0.102f);
    [SerializeField] private UIColor emptyColor = UIColor.Raised;
    [SerializeField] private float blinkSpeed = 2f;

    private int blinkIndex = -1;

    public event Action OnClicked;

    private void Awake()
    {
        button.onClick.AddListener(() => OnClicked?.Invoke());
    }

    private void Update()
    {
        if (blinkIndex < 0) return;

        var color = filledColor;
        color.a = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f));
        stars[blinkIndex].color = color;
    }

    // grade: 선택하면 도달하는 등급(채울 별 개수). 마지막으로 채워지는 별이 깜빡인다
    // category: 무기(Active)/지원(Passive) 구분. 회복 카드처럼 스킬이 아니면 null로 넘겨 배지를 숨긴다
    public void Set(Sprite icon, string title, string description, int grade, bool isNew, SkillCategory? category = null)
    {
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        nameText.text = title;
        descriptionText.text = description;
        newTag.SetActive(isNew);
        if (isNew)
            UITween.Pulse(newTag.transform, 0.15f, 0.4f);
        else
            UITween.StopPulse(newTag.transform);

        SetCategory(category);

        for (var i = 0; i < stars.Length; i++)
            stars[i].color = i < grade ? filledColor : UIPalette.Get(emptyColor);
        blinkIndex = grade > 0 ? grade - 1 : -1;
    }

    private void SetCategory(SkillCategory? category)
    {
        categoryTagImage.gameObject.SetActive(category.HasValue);
        if (!category.HasValue) return;

        var isWeapon = category.Value == SkillCategory.Active;
        categoryTagText.text = isWeapon ? "무기" : "지원";
        categoryTagImage.color = isWeapon ? weaponTagColor : supportTagColor;
    }
}
