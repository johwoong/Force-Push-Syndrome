using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 합성창
public class SynthesisWindow : UIView
{
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private Image resultGradeIcon;
    [SerializeField] private Button resultButton;   // 결과 장비를 누르면 합성 후 상세 미리보기
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text gradeBannerText;
    [SerializeField] private Image gradeBanner;

    [SerializeField] private TMP_Text maxLevelText;
    [SerializeField] private TMP_Text statBonusText;

    [SerializeField] private Image materialItemIcon;
    [SerializeField] private TMP_Text materialCountText;
    [SerializeField] private GameObject combineRow;   // 대상 + 재료 칸 줄 (MAX 일 때 숨김)

    [SerializeField] private Image beforeGradeIcon;
    [SerializeField] private Image beforeItemIcon;
    [SerializeField] private Image materialSlot1GradeIcon;
    [SerializeField] private Image materialSlot1Icon;
    [SerializeField] private Image materialSlot2GradeIcon;
    [SerializeField] private Image materialSlot2Icon;

    [SerializeField] private Button synthesizeButton;
    [SerializeField] private TMP_Text synthesizeButtonText;
    [SerializeField] private Button batchSynthesizeButton;
    [SerializeField] private TMP_Text batchSynthesizeButtonText;
    [SerializeField] private Button backButton;

    [SerializeField] private Color generalColor;
    [SerializeField] private Color superColor;
    [SerializeField] private Color rareColor;

    private OwnedItem boundItem;
    private bool animating;   // 합성 연출 중에는 MAX 여도 합성 줄을 보여 준다
    public OwnedItem BoundItem => boundItem;

    public event System.Action OnShown;

    private void Awake()
    {
        backButton.onClick.AddListener(Close);
        if (resultButton != null)
            resultButton.onClick.AddListener(OnResultClicked);
        synthesizeButton.onClick.AddListener(OnSynthesizeButtonClicked);
        batchSynthesizeButton.onClick.AddListener(OnBatchSynthesizeButtonClicked);
    }

    public void Show(OwnedItem item)
    {
        boundItem = item;
        Open();
        try
        {
            Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        OnShown?.Invoke();
    }

    private void Refresh()
    {
        if (boundItem == null)
            return;

        var data = boundItem.Data;
        var currentIconSprite = Resources.Load<Sprite>(data.iconPath);
        var currentGradeSprite = ItemDatabase.GetGradeIcon(data.SlotType, boundItem.Grade);

        nameText.text = data.itemName;

        gradeBannerText.text = GradeLabel(boundItem.Grade);
        gradeBanner.color = GradeColor(boundItem.Grade);

        var previewGrade = boundItem.CanSynthesize ? boundItem.NextGrade : boundItem.Grade;
        var previewGradeSprite = ItemDatabase.GetGradeIcon(data.SlotType, previewGrade);

        resultItemIcon.sprite = currentIconSprite;
        resultItemIcon.enabled = resultItemIcon.sprite != null;

        resultGradeIcon.sprite = previewGradeSprite;
        resultGradeIcon.enabled = resultGradeIcon.sprite != null;

        materialItemIcon.sprite = currentIconSprite;
        materialItemIcon.enabled = materialItemIcon.sprite != null;

        var owned = PlayerInventory.Instance.CountSynthesisMaterials(boundItem);
        var required = ItemLevelConfig.SynthesisMaterialCount;
        materialCountText.text = boundItem.CanSynthesize
            ? $"필수 재료: {required}x {GradeLabel(boundItem.Grade)} {data.itemName} (보유 {owned}/{required})"
            : "최대 등급이라 더 이상 합성할 수 없습니다";

        beforeGradeIcon.sprite = currentGradeSprite;
        beforeGradeIcon.enabled = beforeGradeIcon.sprite != null;
        beforeItemIcon.sprite = currentIconSprite;
        beforeItemIcon.enabled = beforeItemIcon.sprite != null;

        materialSlot1GradeIcon.sprite = currentGradeSprite;
        materialSlot1GradeIcon.enabled = materialSlot1GradeIcon.sprite != null;
        materialSlot1Icon.sprite = currentIconSprite;
        materialSlot1Icon.enabled = materialSlot1Icon.sprite != null;

        materialSlot2GradeIcon.sprite = currentGradeSprite;
        materialSlot2GradeIcon.enabled = materialSlot2GradeIcon.sprite != null;
        materialSlot2Icon.sprite = currentIconSprite;
        materialSlot2Icon.enabled = materialSlot2Icon.sprite != null;

        if (boundItem.CanSynthesize)
        {
            var next = boundItem.NextGrade;
            maxLevelText.text = $"등급 {GradeLabel(boundItem.Grade)} → {GradeLabel(next)}";

            var bonusPercent = Mathf.RoundToInt((ItemLevelConfig.GetGradeMultiplier(next) - 1f) * 100f);
            statBonusText.text = $"스탯 +{bonusPercent}%";

            var canSynthesize = PlayerInventory.Instance.CanSynthesize(boundItem);
            synthesizeButton.interactable = canSynthesize;
            batchSynthesizeButton.interactable = canSynthesize;
            synthesizeButtonText.text = "합성";
        }
        else
        {
            maxLevelText.text = $"등급 {GradeLabel(boundItem.Grade)} (MAX)";
            statBonusText.text = string.Empty;
            synthesizeButton.interactable = false;
            batchSynthesizeButton.interactable = false;
            synthesizeButtonText.text = "MAX";
        }

        batchSynthesizeButtonText.text = "일괄 합성";

        ApplyMaxLayout();
    }

    // 레어(MAX)면 쓸 수 없는 스탯 증가 줄과 합성 재료 줄을 숨긴다
    private void ApplyMaxLayout()
    {
        var isMax = boundItem != null && !boundItem.CanSynthesize;

        if (statBonusText != null)
            statBonusText.transform.parent.gameObject.SetActive(!isMax);

        if (combineRow != null)
            combineRow.SetActive(!isMax || animating);
    }

    private void OnResultClicked()
    {
        if (boundItem == null) return;

        UIManager.Instance.Get<EquipDetailPopUp>().ShowPreview(boundItem);
    }

    private void OnSynthesizeButtonClicked()
    {
        SetButtonsInteractable(false);
        GameManager.ServerInventory.Synthesize(boundItem, OnRequestFinished);
        
        // if (PlayerInventory.Instance.TrySynthesize(boundItem))
        //     Refresh();
    }

    private void OnBatchSynthesizeButtonClicked()
    {
        SetButtonsInteractable(false);
        GameManager.ServerInventory.BatchSynthesize(boundItem, OnRequestFinished);
        
        // if (PlayerInventory.Instance.BatchSynthesize(boundItem) > 0)
        //     Refresh();
    }

    private void OnRequestFinished(bool success, string message)
    {
        if (!success)
            UIManager.Instance.Get<MessagePopup>().ShowAlert(message);

        Refresh();   // 합성 가능 여부에 따라 버튼 활성화를 다시 결정

        if (success)
        {
            GameManager.Sound.PlaySFX(GameConstants.Sfx.SYNTHESIZE);
            PlaySynthesized();
        }
    }

    // 대상·재료 칸이 결과 아이콘으로 빨려 들어간 뒤 결과가 튀어나온다
    private void PlaySynthesized()
    {
        this.DOKill(true);   // 이전 연출이 남아 있으면 끝내서 칸을 제자리로 돌려 놓는다

        // 마지막 단계(→ 레어)에서도 합성 줄이 연출 동안은 보이도록 한다
        animating = true;
        ApplyMaxLayout();

        var parts = new[] { beforeGradeIcon.transform, materialSlot1GradeIcon.transform, materialSlot2GradeIcon.transform };
        var homes = new Vector3[parts.Length];
        var scales = new Vector3[parts.Length];
        var target = resultGradeIcon.transform.position;
        var sequence = DOTween.Sequence();

        for (var i = 0; i < parts.Length; i++)
        {
            homes[i] = parts[i].localPosition;
            scales[i] = parts[i].localScale;
            sequence.Join(parts[i].DOMove(target, 0.3f).SetEase(Ease.InBack));
            sequence.Join(parts[i].DOScale(scales[i] * 0.3f, 0.3f));
        }

        sequence
            .OnComplete(() =>
            {
                UITween.PopIn(resultItemIcon.transform, 0f, 0.3f, 0.35f);
                UITween.Punch(nameText.transform, 0.2f);
            })
            .OnKill(() =>
            {
                for (var i = 0; i < parts.Length; i++)
                {
                    parts[i].localPosition = homes[i];
                    parts[i].localScale = scales[i];
                }

                animating = false;
                ApplyMaxLayout();
            })
            .SetTarget(this)
            .AsUI(this);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        synthesizeButton.interactable = interactable;
        batchSynthesizeButton.interactable = interactable;
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
