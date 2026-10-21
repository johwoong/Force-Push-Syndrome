using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 카드 UI 하나. 정적 데이터(AchievementData)와 서버 진행도(PlayerAchievementData)를 받아 표시
public class AchievementCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject alertImage;
    
    public AchievementData Data { get; private set; }
    public PlayerAchievementData Progress { get; private set; }

    public event Action<AchievementCard> Clicked;

    private void Awake()
    {
        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void Set(AchievementData data, PlayerAchievementData progress)
    {
        Data = data;
        Progress = progress;

        // 업적 ID로 아이콘을 찾는다. 없으면 이미지를 숨긴다.
        Sprite icon = AchievementIcons.Get(data.achievementId);
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
        
        bool allClaimed = progress.claimedStep >= data.TierCount;
        int step = Mathf.Min(progress.claimedStep, data.TierCount - 1);   // 다음에 받을 단계 (0부터)
        long target = data.GetTargetValue(step);
        long current = Math.Min(progress.progress, target);

        nameText.text = data.achievementName;
        descriptionText.text = data.GetDescription(step);
        UITween.Fill(progressFill, (float)current / target);

        if (allClaimed)
            progressText.text = "완료";
        else if (progress.CanClaim)
            progressText.text = "보상 받기";
        else
            progressText.text = $"{current} / {target}";

        if (alertImage != null)
        {
            alertImage.SetActive(progress.CanClaim);
            if (progress.CanClaim)
                UITween.Pulse(alertImage.transform, 0.15f);
            else
                UITween.StopPulse(alertImage.transform);
        }
    }
}
