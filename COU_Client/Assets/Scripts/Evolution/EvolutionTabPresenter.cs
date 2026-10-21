using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static EvolutionRules;

// 진화 탭에 서버 진화 규칙(4종 순환, 계정 레벨당 4단계 제한)을 표시하고, 해금을 서버 요청으로 처리한다
// EvolutionTab과 같은 오브젝트에 붙인다
[RequireComponent(typeof(EvolutionTab))]
public class EvolutionTabPresenter : MonoBehaviour
{
    [Header("Range")]
    [SerializeField][Min(1)] private int displayMaxAccountLevel = 100;   // 이 계정 레벨까지 노드 표시

    [Header("Icons")]
    [SerializeField] private EvolutionIconSet iconSet;

    private EvolutionTab tab;
    private int currentStep;        // 지금까지 진화한 단계 수 (= 다음 해금 노드 번호)
    private bool isRequesting;
    private bool isPlayerDataSubscribed;

    private void Awake()
    {
        tab = GetComponent<EvolutionTab>();
    }

    private void OnEnable()
    {
        tab.UnlockRequested += HandleUnlockRequested;
        tab.UnlockBlockLabel = GetUnlockBlockLabel;

        if (GameManager.PlayerData != null)
        {
            GameManager.PlayerData.OnPlayerDataChanged += OnPlayerDataChanged;
            isPlayerDataSubscribed = true;
        }

        Rebuild();   // 다른 탭에 갔다 와도 가장 최근 진화 노드가 가운데로
        tab.SetUnlockInteractable(!isRequesting);
    }

    private void OnDisable()
    {
        tab.UnlockRequested -= HandleUnlockRequested;
        tab.UnlockBlockLabel = null;

        if (isPlayerDataSubscribed && PlayerDataManager.HasInstance)
            GameManager.PlayerData.OnPlayerDataChanged -= OnPlayerDataChanged;
        isPlayerDataSubscribed = false;
    }

    private void OnPlayerDataChanged(PlayerSaveData _) => Rebuild();

    // 0단계부터 표시 최대 계정 레벨까지 노드를 만든다
    private void Rebuild()
    {
        var dataManager = GameManager.PlayerData;
        var evolution = EvolutionRules.Data;

        if (dataManager == null || !dataManager.isPlayerDataLoaded || evolution == null)
        {
            Debug.LogWarning("[EvolutionTabPresenter] 계정 데이터 또는 진화 데이터가 없어 진화 탭을 비웁니다.", this);
            tab.SetLinear(System.Array.Empty<EvolutionNodeInfo>(), 0, 0, 1, 1);
            return;
        }

        var saveData = dataManager.currentData;
        int accountLevel = saveData.profile.accountLevel;

        int nodeCount = EvolutionRules.GetMaxStepByAccountLevel(evolution, displayMaxAccountLevel);
        currentStep = Mathf.Min(EvolutionRules.GetStep(saveData.playerStat), nodeCount);

        var nodes = new EvolutionNodeInfo[nodeCount];
        for (int s = 0; s < nodeCount; s++)
            nodes[s] = BuildNode(evolution, s);

        tab.SetLinear(nodes, currentStep, saveData.currency.currencyGold, accountLevel, evolution.evolutionStepsPerLevel);
    }

    // s번째 진화(0부터) 노드. level = 필요 계정 레벨 (모자라면 툴팁에 해금 버튼이 안 나옴)
    private EvolutionNodeInfo BuildNode(EvolutionData evolution, int s)
    {
        if (iconSet == null)
            Debug.LogWarning("[EvolutionTabPresenter] Icon Set이 연결되지 않아 기본 아이콘으로 표시합니다.", this); 

        string statType = EvolutionRules.GetNextStatType(evolution, s);
        int increase = EvolutionRules.GetIncreasePerLevel(evolution, statType);
        string statName = GetStatName(statType);

        return new EvolutionNodeInfo
        {
            level = EvolutionRules.GetRequiredAccountLevel(evolution, s),
            name = statName,
            value = "+" + increase,
            description = statName + "이 " + increase + " 증가합니다.",
            icon = iconSet != null ? iconSet.Get(statType) : null,
            iconColor = Color.white,
            cost = EvolutionRules.GetNextCost(evolution, s)
        };
    }

    // 다음 노드를 지금 진화할 수 없는 이유 (버튼 글자용 짧은 문구, 진화 가능하면 null)
    // 판단 순서는 서버와 같음: 최대 단계 → 계정 레벨 → 골드
    private string GetUnlockBlockLabel(int index)
    {
        if (index != currentStep) return null;
        if (isRequesting) return "진화 중";

        var evolution = EvolutionRules.Data;
        var saveData = GameManager.PlayerData.currentData;

        var block = EvolutionRules.GetBlock(evolution, currentStep,
                                            saveData.profile.accountLevel, saveData.currency.currencyGold);

        return block switch
        {
            EvolutionBlock.MaxStepReached => "진화 완료",
            EvolutionBlock.LevelLocked => "레벨 " + EvolutionRules.GetRequiredAccountLevel(evolution, currentStep) + " 필요",
            EvolutionBlock.NotEnoughGold => "골드 부족",
            _ => null
        };
    }

    private void HandleUnlockRequested(int index)
    {
        if (isRequesting) return;
        if (index != currentStep) return;   // 다음 해금 노드만

        // 서버와 같은 순서로 먼저 확인하고, 막히면 요청 없이 안내
        var saveData = GameManager.PlayerData.currentData;
        var block = EvolutionRules.GetBlock(EvolutionRules.Data, currentStep,
                                            saveData.profile.accountLevel, saveData.currency.currencyGold);
        if (block != EvolutionBlock.None)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", GetBlockMessage(block));
            return;
        }

        isRequesting = true;
        tab.RefreshTooltip();

        // 탭이 닫혀도 요청이 끝까지 처리되도록, 씬 전환에도 유지되는 PlayerDataManager에서 실행
        GameManager.PlayerData.StartCoroutine(EvolutionService.Upgrade(OnUpgraded));
    }

    private void OnUpgraded(ApiResult<EvolutionUpgradeResponse> result)
    {
        if (this == null) return;   // 씬이 바뀌어 진화 탭이 사라진 경우

        isRequesting = false;

        // 진화 레벨·골드 반영과 다시 그리기는 PlayerEvolution → OnPlayerDataChanged → Rebuild에서 처리됨
        if (result.IsSuccess && result.Data.currency != null)
            UIManager.Instance.Get<TopBar>().SetGold(result.Data.currency.currencyGold);

        if (!result.IsSuccess)
            UIManager.Instance.Get<LogPopup>().Show("알림", GetFailMessage(result));

        if (isActiveAndEnabled)
            tab.RefreshTooltip();

        tab.SetUnlockInteractable(true);
        if (result.IsSuccess)
        {
            GameManager.Sound.PlaySFX(GameConstants.Sfx.EVOLVE);
            tab.PlayUnlocked(currentStep - 1);   // Rebuild로 currentStep은 이미 다음 노드를 가리킨다
        }
    }

    // 서버 에러 code를 진화 차단 이유로 바꿔 같은 문구를 사용
    private static string GetFailMessage(ApiResult<EvolutionUpgradeResponse> result)
    {
        if (result.FailType == ApiFailType.ServerError && result.Error != null)
        {
            switch (result.Error.code)
            {
                case EvolutionApi.ErrorMaxStepReached: return GetBlockMessage(EvolutionBlock.MaxStepReached);
                case EvolutionApi.ErrorLevelLocked: return GetBlockMessage(EvolutionBlock.LevelLocked);
                case EvolutionApi.ErrorNotEnoughGold: return GetBlockMessage(EvolutionBlock.NotEnoughGold);
            }
        }
        return result.ToUserMessage("진화에 실패했습니다.");   // PLAYER_PROFILE_NOT_FOUND 등은 서버 안내 문구
    }

    private static string GetBlockMessage(EvolutionBlock block) => block switch
    {
        EvolutionBlock.MaxStepReached => "모든 진화를 완료했습니다.",
        EvolutionBlock.LevelLocked => "계정 레벨을 올리면 더 진화할 수 있습니다.",
        EvolutionBlock.NotEnoughGold => "골드가 부족합니다.",
        _ => "진화할 수 없습니다."
    };

    private static string GetStatName(string statType) => statType switch
    {
        PlayerStatType.Attack => "공격력",
        PlayerStatType.Hp => "체력",
        PlayerStatType.Defense => "방어력",
        PlayerStatType.PotionRecovery => "포션 회복량",
        _ => statType
    };
}