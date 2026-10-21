using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionTab : UIView
{
    [Serializable]
    private class Column
    {
        public EvolutionNodeView nodeTemplate;
        public Image lineTemplate;
        public Color currencyColor;

        [NonSerialized] public EvolutionNodeInfo[] nodes;
        [NonSerialized] public int unlocked;
        [NonSerialized] public int currency;
        [NonSerialized] public List<EvolutionNodeView> views;
        [NonSerialized] public List<Image> lines;
    }

    private const int NodesPerLevel = 3;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button dismissButton;
    [SerializeField] private RectTransform unlockedArea;
    [SerializeField] private EvolutionLevelMark levelMarkTemplate;
    [SerializeField] private Column goldColumn;
    [SerializeField] private Column dnaColumn;
    [SerializeField] private EvolutionTooltip tooltip;
    [SerializeField] private Color unlockedLineColor;
    [SerializeField] private UIColor lockedLineColor = UIColor.Base;
    [SerializeField] private float bottomPadding = 330f;
    [SerializeField] private float topPadding = 330f;
    [SerializeField] private float rowSpacing = 300f;
    [SerializeField] private GameObject dnaColumnRoot;

    public event Action<int> UnlockRequested;
    public Func<int, string> UnlockBlockLabel;

    private bool isLinearMode;
    private readonly List<GameObject> spawned = new();

    private UIManager manager;
    private int accountLevel;
    private Column selectedColumn;
    private int selectedIndex;

    private void Awake()
    {
        dismissButton.onClick.AddListener(tooltip.Hide);
        tooltip.UnlockButton.onClick.AddListener(Unlock);

        manager = UIManager.Instance;
        manager.ThemeChanged += RefreshColumns;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.ThemeChanged -= RefreshColumns;
    }

    public override void Open()
    {
        base.Open();
        UIManager.Instance.Get<TopBar>().ShowCoin(!isLinearMode);
    }

    public override void Close()
    {
        base.Close();
        tooltip.Hide();
        UIManager.Instance.Get<TopBar>().ShowCoin(false);
    }

    public void Set(EvolutionInfo info)
    {
        Clear();
        isLinearMode = false;
        if (dnaColumnRoot != null) dnaColumnRoot.SetActive(true);

        accountLevel = info.accountLevel;
        Setup(goldColumn, info.goldNodes, info.unlockedGoldNodes, info.gold, i => i);
        Setup(dnaColumn, info.dnaNodes, info.unlockedDnaNodes, info.dna, i => LevelRow(info.dnaNodes[i].level));

        var maxLevel = info.goldNodes[^1].level;
        for (var level = 1; level <= maxLevel; level++)
        {
            var mark = Instantiate(levelMarkTemplate, levelMarkTemplate.transform.parent);
            spawned.Add(mark.gameObject);
            mark.gameObject.SetActive(true);
            mark.Set(level, level <= accountLevel);
            Place((RectTransform)mark.transform, RowY(LevelRow(level)));
        }

        var content = scrollRect.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, RowY(LevelRow(maxLevel)) + topPadding);
        unlockedArea.sizeDelta = new Vector2(unlockedArea.sizeDelta.x, RowY(LevelRow(accountLevel)));

        var viewportHeight = scrollRect.viewport.rect.height;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01((unlockedArea.sizeDelta.y - viewportHeight / 2) / (content.sizeDelta.y - viewportHeight));
    }

    private void Setup(Column column, EvolutionNodeInfo[] nodes, int unlocked, int currency, Func<int, int> row)
    {
        column.nodes = nodes;
        column.unlocked = unlocked;
        column.currency = currency;
        column.views = new List<EvolutionNodeView>();
        column.lines = new List<Image>();

        for (var i = 0; i < nodes.Length; i++)
        {
            var index = i;
            var view = Instantiate(column.nodeTemplate, column.nodeTemplate.transform.parent);
            spawned.Add(view.gameObject);
            view.gameObject.SetActive(true);
            view.Button.onClick.AddListener(() => Select(column, index));
            Place((RectTransform)view.transform, RowY(row(i)));
            column.views.Add(view);

            var line = Instantiate(column.lineTemplate, column.lineTemplate.transform.parent);
            spawned.Add(line.gameObject);
            line.gameObject.SetActive(true);
            var from = i == 0 ? 0f : RowY(row(i - 1));
            Place(line.rectTransform, from);
            line.rectTransform.sizeDelta = new Vector2(line.rectTransform.sizeDelta.x, RowY(row(i)) - from);
            column.lines.Add(line);
        }

        Refresh(column);
    }

    private void Refresh(Column column)
    {
        for (var i = 0; i < column.nodes.Length; i++)
        {
            var unlocked = i < column.unlocked;
            column.views[i].Set(column.nodes[i], unlocked);
            column.lines[i].color = unlocked ? unlockedLineColor : UIPalette.Get(lockedLineColor);
        }
    }

    private void RefreshColumns()
    {
        if (goldColumn.views == null)
            return;

        Refresh(goldColumn);
        Refresh(dnaColumn);
    }

    private void Select(Column column, int index)
    {
        selectedColumn = column;
        selectedIndex = index;

        var view = (RectTransform)column.views[index].transform;
        var position = view.anchoredPosition + new Vector2(0f, view.rect.height / 2);

        if (UnlockBlockLabel != null)
        {
            // 다음 노드면 항상 버튼(비용 포함)을 보여 주고, 못 하는 이유가 있으면 비활성 + 사유 표시
            bool isNext = index == column.unlocked;
            string blockLabel = isNext ? UnlockBlockLabel(index) : null;
            tooltip.Show(column.nodes[index], position, isNext, isNext && blockLabel == null, blockLabel, column.currencyColor);
            return;
        }

        tooltip.Show(column.nodes[index], position, CanUnlock(column, index), column.currencyColor);
    }

    private bool CanUnlock(Column column, int index)
    {
        var node = column.nodes[index];
        return index == column.unlocked && node.level <= accountLevel && node.cost <= column.currency;
    }

    private void Unlock()
    {
        if (UnlockRequested != null)
        {
            UnlockRequested.Invoke(selectedIndex);
            return;
        }

        var column = selectedColumn;
        column.currency -= column.nodes[selectedIndex].cost;
        column.unlocked++;
        Refresh(column);

        var topBar = UIManager.Instance.Get<TopBar>();
        if (column == goldColumn)
            topBar.SetGold(column.currency);
        else
            topBar.SetCoin(column.currency);

        Select(column, selectedIndex);
        PlayUnlocked(column, selectedIndex);
    }

    // 서버 해금 성공 후 다시 그려진 골드 열에서 연출한다
    public void PlayUnlocked(int index)
    {
        if (index >= 0 && index < goldColumn.views.Count)
            PlayUnlocked(goldColumn, index);
    }

    // 아래에서 이어지는 선이 차오르고 방금 해금한 노드가 튀어나온다
    private static void PlayUnlocked(Column column, int index)
    {
        var line = column.lines[index].rectTransform;
        line.DOKill(true);
        var size = line.sizeDelta;
        line.sizeDelta = new Vector2(size.x, 0f);
        line.DOSizeDelta(size, 0.25f).SetEase(Ease.OutCubic).AsUI(line);

        UITween.PopIn(column.views[index].transform, 0.15f, 0.5f, 0.35f);
    }

    // 골드 열 하나, 아래부터 순서대로 해금하는 진화 (서버 진화 규칙용)
    // nodesPerLevel마다 오른쪽에 계정 레벨 표시, 바닥부터 현재 계정 레벨까지 도달 영역
    public void SetLinear(EvolutionNodeInfo[] nodes, int unlocked, int gold, int currentAccountLevel, int nodesPerLevel)
    {
        Clear();
        isLinearMode = true;
        accountLevel = currentAccountLevel;   // 노드의 level(필요 계정 레벨)로 해금 가능 여부 판단

        Setup(goldColumn, nodes, unlocked, gold, i => i);

        dnaColumn.nodes = Array.Empty<EvolutionNodeInfo>();
        dnaColumn.views = new List<EvolutionNodeView>();
        dnaColumn.lines = new List<Image>();
        if (dnaColumnRoot != null) dnaColumnRoot.SetActive(false);

        // 계정 레벨 표시: 각 레벨의 마지막 노드 옆
        int maxLevel = nodes.Length / nodesPerLevel;
        for (var level = 1; level <= maxLevel; level++)
        {
            var mark = Instantiate(levelMarkTemplate, levelMarkTemplate.transform.parent);
            spawned.Add(mark.gameObject);
            mark.gameObject.SetActive(true);
            mark.Set(level, level <= accountLevel);
            Place((RectTransform)mark.transform, RowY(level * nodesPerLevel - 1));
        }

        // 높이: 노드 개수 기준
        var content = scrollRect.content;
        var lastRow = Mathf.Max(nodes.Length - 1, 0);
        content.sizeDelta = new Vector2(content.sizeDelta.x, RowY(lastRow) + topPadding);

        // 도달 영역: 현재 계정 레벨 표시까지
        var reachedRow = Mathf.Min(accountLevel * nodesPerLevel - 1, lastRow);
        unlockedArea.sizeDelta = new Vector2(unlockedArea.sizeDelta.x, RowY(reachedRow));

        // 가장 최근에 해금한 노드를 화면 가운데로
        FocusRow(Mathf.Max(unlocked - 1, 0));
    }

    // 지정한 줄(노드)이 화면 가운데에 오도록 스크롤
    public void FocusRow(int row)
    {
        // 탭을 막 켠 직후에는 레이아웃이 계산되지 않아 화면 크기가 0일 수 있음 → 먼저 계산
        Canvas.ForceUpdateCanvases();

        var content = scrollRect.content;
        var viewportHeight = scrollRect.viewport.rect.height;
        var scrollable = Mathf.Max(content.sizeDelta.y - viewportHeight, 1f);
        UITween.ScrollTo(scrollRect, Mathf.Clamp01((RowY(row) - viewportHeight / 2) / scrollable));
    }

    // 서버 요청 중 해금 버튼 잠금
    public void SetUnlockInteractable(bool interactable)
        => tooltip.UnlockButton.interactable = interactable;

    // 툴팁이 열려 있으면 현재 선택한 노드로 다시 표시 (진화 요청 전후로 버튼·문구 갱신)
    public void RefreshTooltip()
    {
        if (selectedColumn == null || selectedColumn.nodes == null) return;
        if (!tooltip.gameObject.activeSelf) return;
        if (selectedIndex < 0 || selectedIndex >= selectedColumn.nodes.Length) return;

        Select(selectedColumn, selectedIndex);
    }

    // 이전에 만든 노드·선·레벨 표시 정리
    private void Clear()
    {
        tooltip.Hide();
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
    }

    private float RowY(int row)
        => bottomPadding + row * rowSpacing;

    private static int LevelRow(int level)
        => level * NodesPerLevel - 1;

    private static void Place(RectTransform target, float y)
        => target.anchoredPosition = new Vector2(target.anchoredPosition.x, y);
}
