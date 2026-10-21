using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectScreen : UIView, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image illustration;
    [SerializeField] private Image prevIllustration;
    [SerializeField] private Image nextIllustration;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button backButton;
    [SerializeField] private float swipeThreshold = 100f;

    private StageData[] stages;
    private Dictionary<int, StageRecordSaveData> records; // PlayerSaveData.stageRecords를 stageId로 조회하기 쉽게 변환한 것
    private int lastUnlockedIndex;                        // 이 인덱스까지 입장할 수 있다
    private int selected;
    private int index;

    private RectTransform illustrationRect;
    private Vector2 illustrationHome;
    private Canvas canvas;

    private void Awake()
    {
        selectButton.onClick.AddListener(Select);
        backButton.onClick.AddListener(Close);

        illustrationRect = illustration.rectTransform;
        illustrationHome = illustrationRect.anchoredPosition;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public override void Open()
    {
        base.Open();
        Show(selected);
    }

    public void SetStages(StageData[] stages, Dictionary<int, StageRecordSaveData> records, int lastUnlockedIndex, int selected)
    {
        this.stages = stages;
        this.records = records;
        this.lastUnlockedIndex = lastUnlockedIndex;
        this.selected = selected;
    }

    // 끄는 동안 일러스트가 손가락을 따라 움직인다
    public void OnDrag(PointerEventData eventData)
    {
        illustrationRect.DOKill();
        var delta = (eventData.position.x - eventData.pressPosition.x) / canvas.scaleFactor;
        illustrationRect.anchoredPosition = illustrationHome + new Vector2(delta, 0f);
    }

    // 넘겼으면 새 일러스트가 넘긴 반대편에서 들어오고, 못 넘겼으면 제자리로 돌아온다
    public void OnEndDrag(PointerEventData eventData)
    {
        var previous = index;
        var delta = eventData.position.x - eventData.pressPosition.x;
        if (delta <= -swipeThreshold)
            Show(index + 1);
        else if (delta >= swipeThreshold)
            Show(index - 1);

        var moved = index - previous;
        if (moved != 0)
        {
            illustrationRect.anchoredPosition = illustrationHome + new Vector2(moved * illustrationRect.rect.width, 0f);
            UITween.PopIn(titleText.transform);
        }

        illustrationRect.DOKill();
        illustrationRect.DOAnchorPos(illustrationHome, 0.25f).SetEase(Ease.OutCubic).AsUI(illustrationRect);
    }

    private void Show(int target)
    {
        index = Mathf.Clamp(target, 0, stages.Length - 1);
        var stage = stages[index];
        titleText.text = stage.stageName;
        illustration.sprite = stage.Icon;
        descriptionText.text = stage.stageDescription;
        selectButton.interactable = index <= lastUnlockedIndex;
        ShowSide(prevIllustration, index - 1);
        ShowSide(nextIllustration, index + 1);
    }

    private void ShowSide(Image side, int sideIndex)
    {
        var exists = sideIndex >= 0 && sideIndex < stages.Length;
        side.gameObject.SetActive(exists);
        if (exists)
            side.sprite = stages[sideIndex].Icon;
    }

    private void Select()
    {
        selected = index;
        var stage = stages[selected];
        records.TryGetValue(stage.stageId, out var record);
        var bestTime = record != null ? record.bestSurvivalSeconds : 0;
        UIManager.Instance.Get<BattleTab>().SetStage(stage, bestTime);
        Close();
    }
}