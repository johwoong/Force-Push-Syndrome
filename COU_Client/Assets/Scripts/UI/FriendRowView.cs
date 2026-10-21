using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 친구 화면의 한 줄 
public class FriendRowView : MonoBehaviour
{
    public static readonly Color BlueButton = new Color(0.2f, 0.6039216f, 0.9411765f);
    public static readonly Color GrayButton = new Color(0.27058825f, 0.3647059f, 0.4509804f);
    public static readonly Color DisabledButton = new Color(0.2f, 0.24f, 0.3f);

    private static readonly Color OnlineColor = new Color(0.55f, 0.9f, 0.62f);
    private static readonly Color OfflineColor = new Color(0.62f, 0.67f, 0.74f);

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private Image onlineDot;
    [SerializeField] private Button primaryButton;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text secondaryLabel;
    [SerializeField] private Button deleteButton;   // 친구삭제버튼
    [SerializeField] private Button rowButton;     

    private const float DefaultTextInset = 340f;    
    private const float OneButtonTextInset = 190f; 
    private const float DeleteTextInset = 120f;   

    public void SetInfo(string nickname, string sub, bool? online)
    {
        primaryButton.gameObject.SetActive(false);
        secondaryButton.gameObject.SetActive(false);
        deleteButton.gameObject.SetActive(false);
        rowButton.onClick.RemoveAllListeners();
        rowButton.enabled = false;
        SetTextRightInset(DefaultTextInset);

        nameText.text = nickname;
        subText.text = sub;
        subText.color = online == true ? OnlineColor : OfflineColor;

        onlineDot.gameObject.SetActive(online.HasValue);
        if (online.HasValue)
            onlineDot.color = online.Value ? OnlineColor : OfflineColor;
    }

    public void SetPrimary(string label, Color color, bool interactable, Action onClick)
        => Setup(primaryButton, primaryLabel, label, color, interactable, onClick);

    public void SetSecondary(string label, Color color, bool interactable, Action onClick)
        => Setup(secondaryButton, secondaryLabel, label, color, interactable, onClick);

    public void HideSecondary()
    {
        secondaryButton.gameObject.SetActive(false);
        SetTextRightInset(OneButtonTextInset);
    }

    public void SetDelete(Action onClick)
    {
        deleteButton.gameObject.SetActive(true);
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => onClick?.Invoke());
        SetTextRightInset(DeleteTextInset);
    }

    public void SetOnClick(Action onClick)
    {
        rowButton.onClick.RemoveAllListeners();
        rowButton.onClick.AddListener(() => onClick?.Invoke());
        rowButton.enabled = true;
    }

    private void SetTextRightInset(float inset)
    {
        SetRightInset(nameText.rectTransform, inset);
        SetRightInset(subText.rectTransform, inset);
    }

    private static void SetRightInset(RectTransform rect, float inset)
    {
        var max = rect.offsetMax;
        max.x = -inset;
        rect.offsetMax = max;
    }

    private static void Setup(Button button, TMP_Text label, string text, Color color, bool interactable, Action onClick)
    {
        button.gameObject.SetActive(true);
        label.text = text;
        button.targetGraphic.color = color;
        button.interactable = interactable;
        button.onClick.RemoveAllListeners();

        if (onClick != null)
            button.onClick.AddListener(() => onClick());
    }
}
