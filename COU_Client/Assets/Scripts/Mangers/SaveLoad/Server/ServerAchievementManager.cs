using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 업적 진행도를 서버에서 받아 보관하고, 보상 수령을 처리한다.
public class ServerAchievementManager : MonoSingleton<ServerAchievementManager>
{
    private readonly Dictionary<int, PlayerAchievementData> progresses = new();
    private bool isLoading;
    private bool isClaiming;
    
    // 기록이 없는 업적은 진행값 0으로 취급한다 (서버와 같은 규칙)
    public PlayerAchievementData GetProgress(int achievementId)
        => progresses.TryGetValue(achievementId, out var progress)
            ? progress
            : new PlayerAchievementData { achievementId = achievementId };
    
    // onComplete(성공 여부, 실패 메시지)
    public void Load(Action<bool, string> onComplete)
    {
        if (isLoading) return;

        isLoading = true;
        StartCoroutine(AchievementApi.GetAchievements(result =>
        {
            isLoading = false;

            if (!result.IsSuccess)
            {
                onComplete?.Invoke(false, result.ToUserMessage("업적 정보를 불러오지 못했습니다."));
                return;
            }

            progresses.Clear();
            foreach (var progress in result.Data)
            {
                progresses[progress.achievementId] = progress;
            }

            onComplete?.Invoke(true, null);
        }));
    }
    
    // onComplete(성공 여부, 실패 메시지)
    public void Claim(int achievementId, Action<bool, string> onComplete)
    {
        if (isClaiming)
        {
            onComplete?.Invoke(false, "요청을 처리하고 있습니다. 잠시만 기다려 주세요.");
            return;
        }

        isClaiming = true;
        StartCoroutine(AchievementApi.Claim(achievementId, result =>
        {
            isClaiming = false;

            if (!result.IsSuccess)
            {
                onComplete?.Invoke(false, result.ToUserMessage("보상을 받지 못했습니다."));
                return;
            }

            progresses[achievementId] = result.Data.achievement;
            ApplyCurrency(result.Data.currency);
            onComplete?.Invoke(true, null);
        }));
    }
    
    // 진화(PlayerEvolution.Apply)와 같은 방식: 서버 재화로 덮어쓰고 변경을 알린다 (TopBar가 갱신된다)
    private static void ApplyCurrency(CurrencyData currency)
    {
        var dataManager = GameManager.PlayerData;
        if (currency == null || dataManager == null || !dataManager.isPlayerDataLoaded)
            return;

        dataManager.currentData.currency = currency;
        dataManager.NotifyPlayerDataChanged();
    }
}
