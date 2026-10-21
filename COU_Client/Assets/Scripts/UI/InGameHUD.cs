using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameHUD : UIView
{
    private const float BarSlideOffset = 200f;   // 경험치바·보스바가 교체될 때 위에서 내려오는 거리

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expFill;
    [SerializeField] private GameObject expGroup;
    [SerializeField] private GameObject bossGroup;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Image bossHpFill;
    [SerializeField] private Image bossHpTrail;   // 보스 HP가 깎일 때 늦게 따라 줄어드는 잔상
    [SerializeField] private Button pauseButton;

    private void Awake()
    {
        pauseButton.onClick.AddListener(() => UIManager.Instance.Open<PauseWindow>());
    }

    public void SetTime(int seconds)
        => timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";

    public void SetGold(int gold)
    {
        goldText.text = gold.ToString();
        UITween.Punch(goldText.transform, 0.08f);
    }

    public void SetKillCount(int kills)
    {
        killText.text = kills.ToString();
        UITween.Punch(killText.transform, 0.08f);
    }

    public void SetLevel(int level)
    {
        var text = $"LV.{level}";
        if (levelText.text == text) return;

        levelText.text = text;
        UITween.Punch(levelText.transform, 0.3f);
    }

    public void SetExp(float ratio)
        => UITween.Fill(expFill, ratio, true, 0.2f);

    public void ShowBoss(string bossName)
    {
        bossNameText.text = bossName;
        expGroup.SetActive(false);
        bossGroup.SetActive(true);

        bossHpTrail.DOKill();
        bossHpTrail.fillAmount = 1f;
        UITween.SlideIn((RectTransform)bossGroup.transform, new Vector2(0f, BarSlideOffset));
    }

    public void SetBossHp(float ratio)
    {
        bossHpFill.fillAmount = ratio;

        bossHpTrail.DOKill();
        if (ratio >= bossHpTrail.fillAmount)
            bossHpTrail.fillAmount = ratio;
        else
            bossHpTrail.DOFillAmount(ratio, 0.4f).SetDelay(0.3f).SetEase(Ease.OutCubic).AsUI(bossHpTrail);
    }

    public void HideBoss()
    {
        bossGroup.SetActive(false);
        expGroup.SetActive(true);
        UITween.SlideIn((RectTransform)expGroup.transform, new Vector2(0f, BarSlideOffset));
    }
}
