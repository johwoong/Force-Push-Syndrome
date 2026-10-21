using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AchievementStepState
{
    Claimed,        // 완료 + 보상 수령함
    Claimable,      // 완료 + 보상 수령할 수 있음
    InProgress,     // 지금 진행 중인 단계
    Locked          // 이전 단계가 안 끝나서 잠김
}

public class Template_AchievementProgress : MonoBehaviour
{
    [SerializeField] private TMP_Text stepText;
    [SerializeField] private Image stateIcon;
    
    [Header("State Icons")]
    [SerializeField] private Sprite claimedSprite;     // Icon_AchievementClaimed
    [SerializeField] private Sprite claimableSprite;   // Icon_AchievementAlert
    [SerializeField] private Sprite inProgressSprite;  // Icon_AchievementInProgress
    [SerializeField] private Sprite lockedSprite;      // Icon_AchievementLocked
    
    [Header("Text Alpha")]
    [SerializeField] private float lockedAlpha = 0.5f;
    [SerializeField] private float claimedAlpha = 0.7f;
    
    public int Step { get; private set; }
    public AchievementStepState State { get; private set; }
    public RectTransform RectTransform => (RectTransform)transform;

    public void Set(AchievementData data, int step, PlayerAchievementData progress)
    {
        Step = step;
        State = GetState(step, progress);

        stateIcon.sprite = GetSprite(State);
        stateIcon.enabled = stateIcon.sprite != null;
        if (State == AchievementStepState.Claimable)
            UITween.Pulse(stateIcon.transform, 0.15f);
        else
            UITween.StopPulse(stateIcon.transform);

        stepText.text = BuildText(data, step, progress);
        stepText.alpha = GetAlpha(State);
    }
    
    // 상태 판단
    private static AchievementStepState GetState(int step, PlayerAchievementData progress)
    {
        if (step < progress.claimedStep) return AchievementStepState.Claimed;
        if (step < progress.completedStep) return AchievementStepState.Claimable;
        if (step == progress.completedStep) return AchievementStepState.InProgress;
        return AchievementStepState.Locked;
    }
    
    // 표시
    private string BuildText(AchievementData data, int step, PlayerAchievementData progress)
    {
        string title = $"<b>{step + 1}단계</b> {data.GetDescription(step)}";
        string reward = $"{GetRewardName(data.GetRewardType(step))} {data.GetRewardAmount(step):N0}";
        string state = GetStateText(data, step, progress);

        return $"{title}\n<size=80%>{reward} · {state}</size>";
    }
    
    private string GetStateText(AchievementData data, int step, PlayerAchievementData progress)
    {
        switch (State)
        {
            case AchievementStepState.Claimed:
                return "수령 완료";
            case AchievementStepState.Claimable:
                return "<color=#7CFC7C>보상 받기 가능</color>";
            case AchievementStepState.InProgress:
                return $"진행 중 ({progress.progress:N0} / {data.GetTargetValue(step):N0})";
            default:
                return "이전 단계 완료 필요";
        }
    }

    private Sprite GetSprite(AchievementStepState state)
    {
        switch (state)
        {
            case AchievementStepState.Claimed: return claimedSprite;
            case AchievementStepState.Claimable: return claimableSprite;
            case AchievementStepState.InProgress: return inProgressSprite;
            default: return lockedSprite;
        }
    }

    private float GetAlpha(AchievementStepState state)
    {
        switch (state)
        {
            case AchievementStepState.Locked: return lockedAlpha;
            case AchievementStepState.Claimed: return claimedAlpha;
            default: return 1f;
        }
    }

    private static string GetRewardName(AchievementRewardType type)
    {
        return type == AchievementRewardType.Gold ? "골드" : "젬";
    }
}
