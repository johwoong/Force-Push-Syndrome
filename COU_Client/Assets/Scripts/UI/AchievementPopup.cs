using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 상세 팝업.
// 단계 목록(Template_AchievementProgress)과 현재 단계 보상(Template_Reward)을 동적으로 만들고,
// 보상 받기를 누르면 서버에 수령 요청 → 계정 재화 반영 → 다음 단계로 갱신한다.
public class AchievementPopup : UIPopup
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;        // Tmp_AchievementDesc
    [SerializeField] private TMP_Text totalProgressText;  // Tmp_AchievementProgress
    [SerializeField] private Button closeButton;

    [Header("Steps")]
    [SerializeField] private ScrollRect stepScroll;                     // ScrollView_AchievementConditions
    [SerializeField] private RectTransform stepContent;                 // Conditions
    [SerializeField] private Template_AchievementProgress stepPrefab;   // Template_AchievementProgress (프리팹 에셋)

    [Header("Reward")]
    [SerializeField] private RectTransform rewardContent;  // Rewards
    [SerializeField] private RewardSlot rewardPrefab;      // Template_Reward (프리팹 에셋)
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite gemIcon;

    [Header("Claim")]
    [SerializeField] private Button claimButton;       // StartButton
    [SerializeField] private TMP_Text claimButtonText;

    private readonly List<Template_AchievementProgress> stepItems = new List<Template_AchievementProgress>();
    private readonly List<RewardSlot> rewardItems = new List<RewardSlot>();

    private AchievementData data;
    private Action onClaimed;
    private bool isClaiming;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        claimButton.onClick.AddListener(OnClaimClick);
    }

    public void Show(AchievementData achievementData, Action onClaimedCallback)
    {
        data = achievementData;
        onClaimed = onClaimedCallback;
        isClaiming = false;

        Open();      // 먼저 활성화해야 레이아웃 계산이 된다
        Refresh();
    }

    private void Refresh()
    {
        PlayerAchievementData progress = GameManager.ServerAchievement.GetProgress(data.achievementId);
        bool isAllClaimed = progress.claimedStep >= data.TierCount;
        int currentStep = Mathf.Min(progress.claimedStep, data.TierCount - 1);   // 다음에 받을 단계

        titleText.text = data.achievementName;
        summaryText.text = BuildSummary();
        totalProgressText.text = $"{BuildTotal(progress.progress)} · {progress.completedStep} / {data.TierCount} 단계 달성";

        BuildSteps(progress);
        BuildRewards(currentStep, isAllClaimed);
        RefreshClaimButton(progress, isAllClaimed);
        ScrollToStep(currentStep);
    }

    // ---------- 단계 행 ----------

    private void BuildSteps(PlayerAchievementData progress)
    {
        EnsureCount(stepItems, stepPrefab, stepContent, data.TierCount);

        for (int i = 0; i < data.TierCount; i++)
            stepItems[i].Set(data, i, progress);
    }

    private void ScrollToStep(int step)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(stepContent);

        float viewHeight = stepScroll.viewport.rect.height;
        float scrollable = stepContent.rect.height - viewHeight;
        if (scrollable <= 0f)   // 스크롤할 게 없으면 맨 위
        {
            stepScroll.verticalNormalizedPosition = 1f;
            return;
        }

        RectTransform row = stepItems[step].RectTransform;
        Vector3 rowCenter = stepContent.InverseTransformPoint(row.TransformPoint(row.rect.center));
        float distanceFromTop = -rowCenter.y;   // Conditions pivot y = 1 기준

        float offset = Mathf.Clamp(distanceFromTop - viewHeight * 0.5f, 0f, scrollable);
        UITween.ScrollTo(stepScroll, 1f - offset / scrollable);
    }

    // ---------- 보상 ----------

    // 현재 단계의 보상을 Template_Reward로 만든다.
    // 받을 수 있으면 받을 보상, 진행 중이면 이번 단계 보상 미리보기, 전부 받았으면 비운다.
    private void BuildRewards(int step, bool isAllClaimed)
    {
        List<(AchievementRewardType type, int amount)> rewards = isAllClaimed
            ? new List<(AchievementRewardType, int)>()
            : GetStepRewards(step);

        EnsureCount(rewardItems, rewardPrefab, rewardContent, rewards.Count);

        for (int i = 0; i < rewards.Count; i++)
            rewardItems[i].Set(GetRewardIcon(rewards[i].type), rewards[i].amount);
    }

    // 지금 시트는 단계당 보상 1개. 나중에 한 단계에 보상이 여러 개가 되면 여기만 바꾸면 된다
    private List<(AchievementRewardType type, int amount)> GetStepRewards(int step)
    {
        return new List<(AchievementRewardType, int)>
        {
            (data.GetRewardType(step), data.GetRewardAmount(step))
        };
    }

    private Sprite GetRewardIcon(AchievementRewardType type)
    {
        return type == AchievementRewardType.Gold ? goldIcon : gemIcon;
    }

    // ---------- 수령 ----------

    private void RefreshClaimButton(PlayerAchievementData progress, bool isAllClaimed)
    {
        claimButton.interactable = progress.CanClaim && !isClaiming;

        if (isAllClaimed) claimButtonText.text = "완료";
        else if (progress.CanClaim) claimButtonText.text = "보상 받기";
        else claimButtonText.text = "진행 중";
    }

    private void OnClaimClick()
    {
        if (isClaiming) return;

        // 받을 단계 보상을 미리 기록 (수령 후엔 claimedStep이 올라간다)
        int step = GameManager.ServerAchievement.GetProgress(data.achievementId).claimedStep;
        var rewards = GetStepRewards(step);

        isClaiming = true;
        claimButton.interactable = false;   // 연타 방지

        GameManager.ServerAchievement.Claim(data.achievementId, (success, message) =>
        {
            isClaiming = false;

            if (!success)
            {
                Debug.LogWarning($"[AchievementPopup] {data.achievementName} 보상 수령 실패: {message}");
                if (IsOpen) Refresh();
                return;
            }

            GameManager.Sound.PlaySFX(GameConstants.Sfx.ACHIEVEMENT);

            // 재화는 ServerAchievementManager.ApplyCurrency에서 이미 계정에 반영됨 (TopBar 갱신)
            var currency = GameManager.PlayerData.currentData.currency;
            foreach (var reward in rewards)
                Debug.Log($"[AchievementPopup] {data.achievementName} {step + 1}단계 보상: {reward.type} {reward.amount:N0}" +
                          $" (보유 골드 {currency.currencyGold:N0}, 보석 {currency.currencyGem:N0})");

            onClaimed?.Invoke();        // 탭의 카드 갱신
            if (!IsOpen) return;

            Refresh();                  // 다음 단계로 팝업 갱신
            UITween.Punch(totalProgressText.transform);
            UITween.Stagger(rewardItems, 0.06f);   // 다음 단계 보상이 새로 나온다
        });
    }

    // ---------- 공용 ----------

    // 모자라면 생성, 남으면 비활성화 (Destroy 대신 재사용)
    private static void EnsureCount<T>(List<T> list, T prefab, RectTransform parent, int count) where T : Component
    {
        while (list.Count < count)
            list.Add(Instantiate(prefab, parent));

        for (int i = 0; i < list.Count; i++)
            list[i].gameObject.SetActive(i < count);
    }

    // ---------- 상단 텍스트 ----------

    private string BuildSummary()
    {
        switch (data.ConditionType)
        {
            case AchievementConditionType.KillBySkill:
                return $"{GetSkillName(data.targetSkillId)}(으)로 몬스터 처치";
            case AchievementConditionType.PickupDropItem:
                return $"{GetDropItemName(data.TargetDropItemType)} 획득";
            case AchievementConditionType.UpgradeSkill:
                return data.targetSkillId != 0 ? $"{GetSkillName(data.targetSkillId)} 강화" : "스킬 강화";
            case AchievementConditionType.GainBattleExp:
                return "전투 경험치 획득";
            case AchievementConditionType.GainBattleGold:
                return "전투에서 골드 획득";
            case AchievementConditionType.ClearStage:
                return "스테이지 클리어";
            case AchievementConditionType.PlayLuckTrain:
                return "행운열차 완주";
            default:
                return string.Empty;
        }
    }

    private string BuildTotal(long value)
    {
        switch (data.ConditionType)
        {
            case AchievementConditionType.KillBySkill: return $"누적 {value:N0}마리";
            case AchievementConditionType.GainBattleExp: return $"누적 경험치 {value:N0}";
            case AchievementConditionType.GainBattleGold: return $"누적 골드 {value:N0}";
            case AchievementConditionType.ClearStage: return $"누적 {value:N0}회 클리어";
            default: return $"누적 {value:N0}회";
        }
    }

    private static string GetSkillName(long skillId)
    {
        return GameManager.JsonData.SkillDataDic.TryGetValue(skillId, out SkillData skill)
            ? skill.skillDescription
            : $"스킬({skillId})";
    }

    private static string GetDropItemName(DropItemType type)
    {
        switch (type)
        {
            case DropItemType.Bomb: return "폭탄";
            case DropItemType.Magnet: return "자석";
            case DropItemType.Potion: return "회복약";
            default: return type.ToString();
        }
    }
}