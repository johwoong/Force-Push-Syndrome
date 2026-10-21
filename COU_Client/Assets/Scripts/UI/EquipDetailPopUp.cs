using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipDetailPopUp : UIPopup
{
    [SerializeField] private Image gradeBanner;
    [SerializeField] private TMP_Text gradeBannerText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image gradeIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text skillListText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TMP_Text levelUpButtonText;
    [SerializeField] private Button batchLevelUpButton;
    [SerializeField] private TMP_Text batchLevelUpButtonText;
    [SerializeField] private Button closeButton;

    [SerializeField] private Color generalColor;
    [SerializeField] private Color superColor;
    [SerializeField] private Color rareColor;
    [SerializeField] private Color skillBulletColor = new(0.31f, 0.76f, 0.97f);

    private OwnedItem boundItem;
    private bool isPreview;   // 합성 결과 미리보기: 읽기 전용이라 장착/레벨업을 숨기고 닫기만 남긴다
    private RectTransform equipButtonRect;
    private Vector2 equipButtonHomePos;
    private Vector2 equipButtonPreviewPos;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        equipButton.onClick.AddListener(OnEquipButtonClicked);
        levelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
        batchLevelUpButton.onClick.AddListener(OnBatchLevelUpButtonClicked);
    }

    public void Show(OwnedItem item)
    {
        boundItem = item;
        isPreview = false;
        Refresh();
        Open();
    }

    // 합성 결과 미리보기: 같은 장비·레벨에 다음 등급을 준 임시 장비로 상세를 보여 준다 (서버/인벤토리에는 없는 장비)
    public void ShowPreview(OwnedItem source)
    {
        var grade = source.CanSynthesize ? source.NextGrade : source.Grade;
        boundItem = new OwnedItem(source.itemId) { level = source.level, grade = grade };
        isPreview = true;
        Refresh();
        Open();
    }

    private void Refresh()
    {
        var data = boundItem.Data;

        nameText.text = data.itemName;
        descriptionText.text = data.description;

        gradeBannerText.text = GradeLabel(boundItem.Grade);
        gradeBanner.color = GradeColor(boundItem.Grade);

        itemIcon.sprite = Resources.Load<Sprite>(data.iconPath);
        itemIcon.enabled = itemIcon.sprite != null;

        // 아이템슬롯(ItemSlotView)과 동일하게, 아이콘 뒤 프레임을 등급별 아이콘/테두리로 교체.
        gradeIcon.sprite = ItemDatabase.GetGradeIcon(data.SlotType, boundItem.Grade);
        gradeIcon.enabled = gradeIcon.sprite != null;

        // 공격력이 있으면 공격력만, 체력이 있으면 체력만 (둘 다 있으면 둘 다) 표시.
        // 레벨업으로 올라간 실제 스탯(ScaledAttack/ScaledHp)을 그대로 반영한다.
        atkText.transform.parent.gameObject.SetActive(data.attackBonus > 0);
        atkText.text = $"ATK {boundItem.ScaledAttack}";

        hpText.transform.parent.gameObject.SetActive(data.hpBonus > 0);
        hpText.text = $"HP {boundItem.ScaledHp}";

        levelText.text = $"레벨: {boundItem.level}/{boundItem.MaxLevel}";

        // 신규 장비 등급 효과 데이터로 설명 문구 구성
        // C6에서 등급별 UI 및 색상 표현을 정식으로 구현한다.
        var skillLines = new List<string>();

        foreach (ItemGrade grade in new[]
        {
            ItemGrade.General,
            ItemGrade.Super,
            ItemGrade.Rare
        })
        {
            var effects = GameManager.JsonData
                .GetItemGradeEffectsFromJson(boundItem.itemId, grade);

            foreach (var effect in effects)
            {
                // 79xxx 제외 효과는 화면에 표시하지 않는다.
                if (effect.IsExcluded)
                    continue;

                string description =
                    $"{GradeLabel(grade)}: {effect.effectDescription}";

                // 현재 장비 인스턴스 등급 이하이면 해금
                if (grade <= boundItem.Grade)
                {
                    skillLines.Add(FormatSkillLine(description));
                }
                else
                {
                    // 아직 해금되지 않은 효과
                    skillLines.Add(
                        $"<color=#888888>■ [잠금] {description}</color>");
                }
            }
        }

        // 장비 등급 효과: 등급별 제목 + 효과 줄, 현재 등급보다 높은 등급은 잠금
        skillListText.text = BuildGradeEffectText();

        RefreshLevelUpArea();
        RefreshEquipButton();
        RefreshPreviewLayout();
    }

    // 미리보기에서는 골드/레벨업 영역을 숨기고, 장착 버튼 자리를 가운데 "닫기" 버튼으로 쓴다
    private void RefreshPreviewLayout()
    {
        if (equipButtonRect == null)
        {
            equipButtonRect = (RectTransform)equipButton.transform;
            equipButtonHomePos = equipButtonRect.anchoredPosition;
            equipButtonPreviewPos = ((RectTransform)levelUpButton.transform).anchoredPosition;
        }

        goldText.transform.parent.gameObject.SetActive(!isPreview);
        levelUpButton.gameObject.SetActive(!isPreview);
        batchLevelUpButton.gameObject.SetActive(!isPreview);
        equipButtonRect.anchoredPosition = isPreview ? equipButtonPreviewPos : equipButtonHomePos;
    }

    private static readonly ItemGrade[] GradeOrder = { ItemGrade.General, ItemGrade.Super, ItemGrade.Rare };

    // 등급별 제목 줄 + 효과 한 줄 (같은 등급의 효과는 " · "로 이어 붙임)
    // - 인스턴스 등급 이하: 등급 색 제목, 등급 색 박스
    // - 높은 등급: 회색 제목 + [잠금], 박스·문구 모두 회색
    // - 제외(79xxx)는 숨기고, 표시할 효과가 없는 등급은 제목도 생략
    private string BuildGradeEffectText()
    {
        var lines = new List<string>();
        string lockedHex = ColorUtility.ToHtmlStringRGB(UIPalette.Get(UIColor.SubContent));

        foreach (var grade in GradeOrder)
        {
            var effects = GameManager.JsonData.GetItemGradeEffectsFromJson(boundItem.itemId, grade);

            var descriptions = new List<string>();
            foreach (var effect in effects)
            {
                if (effect != null && !effect.IsExcluded)
                    descriptions.Add(effect.effectDescription);
            }

            if (descriptions.Count == 0)
                continue;

            string text = string.Join(" · ", descriptions);
            bool unlocked = grade <= boundItem.Grade;

            if (unlocked)
            {
                string gradeHex = ColorUtility.ToHtmlStringRGB(GradeColor(grade));
                lines.Add($"<color=#{gradeHex}><b>{GradeLabel(grade)}</b></color>");
                lines.Add($" <color=#{gradeHex}>■</color> {text}");
            }
            else
            {
                lines.Add($"<color=#{lockedHex}><b>{GradeLabel(grade)}</b>  [잠금]</color>");
                lines.Add($" <color=#{lockedHex}>■ {text}</color>");
            }
        }

        return string.Join("\n", lines);
    }

    private string FormatSkillLine(string skill)
        => $"<color=#{ColorUtility.ToHtmlStringRGB(skillBulletColor)}>■</color> {skill}";

    private void RefreshLevelUpArea()
    {
        var playerGold = PlayerInventory.Instance.Gold;

        if (boundItem.CanLevelUp)
        {
            var cost = boundItem.NextLevelUpCost;
            goldText.text = $"{FormatGold(playerGold)}/{FormatGold(cost)}";

            var canAfford = playerGold >= cost;
            levelUpButton.interactable = canAfford;
            batchLevelUpButton.interactable = canAfford;
            levelUpButtonText.text = "레벨업";
        }
        else
        {
            goldText.text = $"{FormatGold(playerGold)} (MAX)";
            levelUpButton.interactable = false;
            batchLevelUpButton.interactable = false;
            levelUpButtonText.text = "MAX";
        }

        batchLevelUpButtonText.text = "일괄 레벨업";
    }

    private static string FormatGold(int amount)
        => amount >= 10000 ? $"{amount / 1000f:0.#}K" : amount.ToString();

    private void OnLevelUpButtonClicked()
    {
        SetButtonsInteractable(false);
        GameManager.ServerInventory.LevelUp(boundItem, OnRequestFinished);

        // if (PlayerInventory.Instance.TryLevelUp(boundItem))
        //     Refresh();
    }

    private void OnBatchLevelUpButtonClicked()
    {
        SetButtonsInteractable(false);
        GameManager.ServerInventory.BatchLevelUp(boundItem, OnRequestFinished);
        
        // if (PlayerInventory.Instance.BatchLevelUp(boundItem) > 0)
        //     Refresh();
    }

    private void OnEquipButtonClicked()
    {
        if (isPreview)
        {
            Close();
            return;
        }

        SetButtonsInteractable(false);

        if (boundItem.isEquipped)
            GameManager.ServerInventory.Unequip(boundItem, (success, message) =>
            {
                if (success) GameManager.Sound.PlaySFX(GameConstants.Sfx.UNEQUIP);
                OnRequestFinished(success, message);
            });
        else
            GameManager.ServerInventory.Equip(boundItem, (success, message) =>
            {
                // 서버가 장착을 승인했을 때만 효과음
                if (success) GameManager.Sound.PlaySFX(GameConstants.Sfx.EQUIP);
                OnRequestFinished(success, message);
            });
        
        // if (boundItem.isEquipped)
        //     PlayerInventory.Instance.Unequip(boundItem.Data.SlotType);
        // else
        //     PlayerInventory.Instance.Equip(boundItem);
        //
        // RefreshEquipButton();
    }

    // 성공/실패 모두 서버 결과 기준으로 화면을 다시 그린다 (버튼 활성화도 Refresh에서 결정)
    private void OnRequestFinished(bool success, string message)
    {
        if (!success)
            UIManager.Instance.Get<MessagePopup>().ShowAlert(message);

        var shownLevel = levelText.text;
        Refresh();

        // 레벨이 올랐으면 레벨과 스탯이 튕긴다
        if (levelText.text == shownLevel) return;
        GameManager.Sound.PlaySFX(GameConstants.Sfx.EQUIP_LEVEL_UP);
        UITween.Punch(levelText.transform, 0.25f);
        UITween.Punch(atkText.transform, 0.25f, 0.05f);
        UITween.Punch(hpText.transform, 0.25f, 0.1f);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        equipButton.interactable = interactable;
        levelUpButton.interactable = interactable;
        batchLevelUpButton.interactable = interactable;
    }

    private void RefreshEquipButton()
    {
        equipButton.interactable = true;
        equipButtonText.text = isPreview ? "닫기" : boundItem.isEquipped ? "장착 해제" : "장착";
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
