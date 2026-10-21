using System;
using System.Linq;
using UnityEngine;

public class MainUIAccountBinder : MonoBehaviour
{
    private void Start()
    {
        GameManager.PlayerData.OnPlayerDataChanged += RefreshAll;

        if (GameManager.PlayerData.isPlayerDataLoaded)
            RefreshAll(GameManager.PlayerData.currentData);
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.HasInstance)
            GameManager.PlayerData.OnPlayerDataChanged -= RefreshAll;
    }

    private void RefreshAll(PlayerSaveData data)
    {
        RefreshTopBar(data);
        RefreshStageSelect(data);
    }

    private void RefreshTopBar(PlayerSaveData data)
    {
        var topBar = GameManager.UI.Get<TopBar>();
        var accountConst = GameManager.JsonData.AccountConstData;
        var profile = data.profile;

        topBar.SetNickname(profile.playerNickname);
        topBar.SetLevel(profile.accountLevel);
        topBar.SetExp(profile.accountLevel >= accountConst.maxAccountLevel
            ? 1f
            : profile.accountExp / (float)accountConst.GetRequiredExp(profile.accountLevel));
        // 최대 스태미나는 저장하지 않는 정적 값이라 AccountConst에서 읽는다
        topBar.SetStamina(data.currency.currencyEnergy, accountConst.maxStamina);
        topBar.SetGold(data.currency.currencyGold);
        topBar.SetGem(data.currency.currencyGem);
    }

    private void RefreshStageSelect(PlayerSaveData data)
    {
        var stageDataDic = GameManager.JsonData.StageDataDic; // 시트에서 온 공용 데이터
        if (stageDataDic == null || stageDataDic.Count == 0) return;

        var stages = stageDataDic.Values.OrderBy(s => s.stageId).ToArray();
        var progress = data.stageProgress;

        var records = data.stageRecords.ToDictionary(record => record.stageId);

        // 순서대로 해금되므로 클리어한 스테이지 수가 곧 입장할 수 있는 마지막 인덱스 (최고 클리어의 다음 스테이지)
        int lastUnlockedIndex = stages.Count(stage => progress.IsCleared(stage.stageId));

        // 마지막으로 진행한 스테이지(currentStageId)를 기본 선택으로
        int selectedIndex = Math.Max(0, Array.FindIndex(stages, s => s.stageId == progress.currentStageId));

        GameManager.UI.Get<StageSelectScreen>().SetStages(stages, records, lastUnlockedIndex, selectedIndex);

        var selectedStage = stages[selectedIndex];
        int bestTime = records.TryGetValue(selectedStage.stageId, out var record) ? record.bestSurvivalSeconds : 0;

        var battleTab = GameManager.UI.Get<BattleTab>();
        battleTab.SetStage(selectedStage, bestTime);
        battleTab.SetStaminaCost(GameManager.JsonData.AccountConstData.battleStaminaCost);

        // 클리어했지만 아직 받지 않은 가장 앞 스테이지의 보물상자
        var chestStage = stages.FirstOrDefault(stage =>
            progress.IsCleared(stage.stageId) && !data.claimedChapterChests.Contains(stage.stageId));
        battleTab.SetChapterChest(chestStage != null ? chestStage.stageId : 0);
        battleTab.SetPatrolUnlocked(progress.maxClearedStageId.HasValue);
    }
}