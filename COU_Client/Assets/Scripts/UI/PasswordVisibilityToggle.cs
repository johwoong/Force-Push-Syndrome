using System;
using NUnit.Framework.Internal.Commands;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 비밀번호 입력칸의 * 표시를 켜고 끄는 버튼 (InputField_Password/Btn_Visible)
// * 표시 켜짐 -> 눈동자 아이콘 / * 표시 꺼짐 -> 빗금 눈동자 아이콘
[RequireComponent(typeof(Button))]
public class PasswordVisibilityToggle : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Image iconImage;

    private Sprite maskedSprite;
    private Sprite unmaskedSprite;

    private bool isMasked = true;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        LoadIcons();
        GetComponent<Button>().onClick.AddListener(Toggle);
        Apply();
    }

    public void Toggle()
    {
        isMasked = !isMasked;
        Apply();
    }

    private void Apply()
    {
        // Password: asteriskChar(기본 '*')로 표시, Standard: 그대로 표시
        passwordInputField.contentType = isMasked
            ? TMP_InputField.ContentType.Password
            : TMP_InputField.ContentType.Standard;
        passwordInputField.ForceLabelUpdate(); // 입력해 둔 글자에도 바로 반영

        Sprite icon = isMasked ? maskedSprite : unmaskedSprite;
        if (icon != null)
            iconImage.sprite = icon;
    }
    
    private void LoadIcons()
    {
        foreach (Sprite sprite in Resources.LoadAll<Sprite>(GameConstants.Paths.LOGIN_PASSWORD_ICON_SHEET))
        {
            if (sprite.name == GameConstants.Paths.LOGIN_PASSWORD_ICON_MASKED) maskedSprite = sprite;
            else if (sprite.name == GameConstants.Paths.LOGIN_PASSWORD_ICON_UNMASKED) unmaskedSprite = sprite;
        }

        if (maskedSprite == null || unmaskedSprite == null)
            Debug.LogWarning($"[PasswordVisibilityToggle] 아이콘을 찾지 못했습니다: {GameConstants.Paths.LOGIN_PASSWORD_ICON_SHEET}");
    }
}
