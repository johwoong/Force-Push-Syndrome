using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 스탯 상세 팝업: 메인 화면 기준 최종값 (기본 + 진화 + 장비 + 장비 등급 효과)
// 전투 중에만 생기는 값(스킬, 패시브 보너스, 전투 레벨)은 포함하지 않는다
public class PlayerStatDetailPopup : UIPopup
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;
    [SerializeField] private PlayerStatRowView rowTemplate;   // 꺼 둔 행 템플릿 (Content 아래)

    private readonly List<PlayerStatRowView> rows = new();
    private readonly List<(string label, string value)> entries = new();
    private bool isSubscribed;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        if (dimButton != null)
            dimButton.onClick.AddListener(Close);

        rowTemplate.gameObject.SetActive(false);
    }

    public override void Open()
    {
        base.Open();
        Refresh();
    }

    private void OnEnable()
    {
        // 열려 있는 동안 장비·진화가 바뀌면 다시 그림
        if (GameManager.PlayerData != null)
        {
            GameManager.PlayerData.OnPlayerDataChanged += OnPlayerDataChanged;
            isSubscribed = true;
        }
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged += Refresh;
    }

    private void OnDisable()
    {
        if (isSubscribed && PlayerDataManager.HasInstance)
            GameManager.PlayerData.OnPlayerDataChanged -= OnPlayerDataChanged;
        isSubscribed = false;

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void OnPlayerDataChanged(PlayerSaveData _) => Refresh();

    private void Refresh()
    {
        BuildEntries();

        // 필요한 만큼 행을 만들고, 남는 행은 끔
        while (rows.Count < entries.Count)
        {
            var row = Instantiate(rowTemplate, rowTemplate.transform.parent);
            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < entries.Count;
            rows[i].gameObject.SetActive(used);
            if (used)
                rows[i].Set(entries[i].label, entries[i].value);
        }
    }

    private void BuildEntries()
    {
        entries.Clear();

        var dataManager = GameManager.PlayerData;
        if (dataManager == null || !dataManager.isPlayerDataLoaded)
            return;

        var baseStat = GameManager.JsonData.PlayerBaseStatData;
        var stat = PlayerStatCalculator.CalculateCurrent();
        var fx = stat.Effects;

        // 기본
        Add("전투력", stat.HasCombatPowerData ? N(stat.CombatPower) : "-");
        Add("공격력", N(stat.FinalAttack));
        Add("최대 체력", N(stat.FinalHp));
        Add("방어력", N(stat.FinalDefense));

        // 전투 (전투 중 패시브 보너스는 제외)
        Add("치명타 확률", (baseStat.playerBaseCriticalChance + fx.CriticalChance) + "%");
        Add("치명타 피해", (baseStat.playerBaseCriticalDamage + fx.CriticalDamage) + "%");
        Add("스킬 피해", baseStat.playerBaseSkillDamage + "%");

        // 생존
        Add("회피", fx.DodgeChance + "%");
        Add("받는 피해 감소", fx.DamageReduction + "%");
        Add("피해 무효 확률", F(fx.NegateDamageChance) + "%");
        Add("받는 치유량", "+" + fx.HealReceived + "%");
        Add("포션 회복량", "+" + stat.FinalPotionRecovery);
        Add("주기 회복", fx.IntervalHealPercent > 0f && fx.IntervalHealSeconds > 0f
            ? F(fx.IntervalHealSeconds) + "초마다 " + F(fx.IntervalHealPercent) + "%"
            : "없음");

        // 이동·수집
        Add("이동 속도", stat.FinalMoveSpeed.ToString("0.00", CultureInfo.InvariantCulture));
        Add("이동 속도 상한", F(baseStat.playerBaseMaxMoveSpeed));
        Add("루팅 범위", baseStat.playerBaseLootRadius.ToString("0.0", CultureInfo.InvariantCulture));
    }

    private void Add(string label, string value) => entries.Add((label, value));

    // 천 단위 쉼표 정수
    private static string N(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    // 소수가 있을 때만 소수점 (1.5 → "1.5", 5 → "5")
    private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}