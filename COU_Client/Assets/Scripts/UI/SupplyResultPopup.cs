using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupplyResultPopup : UIPopup
{
    [SerializeField] private Button dimButton;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image gradeFrameImage;
    [SerializeField] private Image itemIconImage;

    [SerializeField] private Color generalColor;
    [SerializeField] private Color superColor;
    [SerializeField] private Color rareColor;

    private void Awake()
    {
        dimButton.onClick.AddListener(Close);
    }

    public void Show(OwnedItem item)
    {
        var data = item.Data;
        var grade = item.Grade;

        gradeText.text = GradeLabel(grade);
        gradeText.color = GradeColor(grade);
        nameText.text = data.itemName;
        nameText.color = GradeColor(grade);

        itemIconImage.sprite = Resources.Load<Sprite>(data.iconPath);
        itemIconImage.enabled = itemIconImage.sprite != null;

        gradeFrameImage.sprite = ItemDatabase.GetGradeIcon(data.SlotType, grade);
        gradeFrameImage.enabled = gradeFrameImage.sprite != null;

        Open();
        PlayReveal(grade);
    }

    // 상자에서 꺼내듯 아이콘이 튀어나오고 등급·이름이 뒤따른다. 등급이 높을수록 크게 흔들린다
    private void PlayReveal(ItemGrade grade)
    {
        var frame = gradeFrameImage.transform;
        UITween.PopIn(frame, 0f, 0f, 0.4f);

        var shake = grade switch
        {
            ItemGrade.Rare => 20f,
            ItemGrade.Super => 10f,
            _ => 0f
        };
        if (shake > 0f)
            frame.DOPunchRotation(new Vector3(0f, 0f, shake), 0.5f, 10).SetDelay(0.35f).AsUI(frame);

        UITween.PopIn(gradeText.transform, 0.25f, 0f);
        UITween.PopIn(nameText.transform, 0.35f, 0f);
    }

    private static string GradeLabel(ItemGrade grade) => grade switch
    {
        ItemGrade.General => "일반",
        ItemGrade.Super => "우수",
        ItemGrade.Rare => "레어",
        _ => grade.ToString()
    };

    private Color GradeColor(ItemGrade grade) => grade switch
    {
        ItemGrade.General => generalColor,
        ItemGrade.Super => superColor,
        ItemGrade.Rare => rareColor,
        _ => Color.white
    };
}
