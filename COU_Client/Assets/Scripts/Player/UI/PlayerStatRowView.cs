using TMPro;
using UnityEngine;

// 스탯 상세 팝업의 행 하나: 이름(왼쪽) + 값(오른쪽)
public class PlayerStatRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    public void Set(string label, string value)
    {
        nameText.text = label;
        valueText.text = value;
    }
}