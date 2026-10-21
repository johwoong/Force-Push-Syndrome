using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultWindow : UIView
{
    [SerializeField] private Image banner;
    [SerializeField] private Image ribbon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Color victoryColor;
    [SerializeField] private Color defeatColor;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private GameObject box;
    [SerializeField] private TMP_Text boxCountText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        confirmButton.onClick.AddListener(() => GameManager.Scene.ChangeScene(GameConstants.SceneNames.MAIN_SCENE));
    }

    // 배너가 크게 찍히듯 내려앉고 리본이 펼쳐진다
    public void Show(bool victory)
    {
        titleText.text = victory ? "승리" : "실패";
        banner.color = ribbon.color = victory ? victoryColor : defeatColor;
        Open();

        GameManager.Sound.StopBGM(0.5f);
        GameManager.Sound.PlaySFX(victory ? GameConstants.Sfx.STAGE_CLEAR : GameConstants.Sfx.STAGE_FAIL);

        UITween.PopIn(banner.transform, 0f, 1.6f, 0.35f);
        UITween.PopIn(ribbon.transform, 0.15f, 0f, 0.3f);
    }

    public void SetTime(int seconds)
        => UITween.Count(timeText, 0, seconds, FormatTime, 0.8f);

    public void SetChapter(int chapter)
        => chapterText.text = $"{chapter} 챕터";

    public void SetBestTime(int seconds)
        => bestTimeText.text = $"최고:<color=#A8F000>{FormatTime(seconds)}</color>";

    public void SetKillCount(int kills)
        => UITween.Count(killText, 0, kills, value => value.ToString(), 0.8f);

    public void SetBoxCount(int count)
    {
        box.SetActive(count > 0);
        boxCountText.text = $"x{count}";
        if (count > 0)
            UITween.PopIn(box.transform, 0f, 0f);
    }

    public void SetGold(int gold)
    {
        UITween.StopBlink(goldText);
        UITween.Count(goldText, 0, gold, value => $"x{FormatAmount(value)}", 0.8f);
    }

    public void SetExp(int exp)
    {
        UITween.StopBlink(expText);
        UITween.Count(expText, 0, exp, value => $"x{FormatAmount(value)}", 0.8f);
    }

    // 서버 응답을 기다리는 동안 지급값을 비워 깜빡이고 확인 버튼을 막는다
    public void SetWaiting(bool waiting)
    {
        confirmButton.interactable = !waiting;
        if (!waiting) return;

        box.SetActive(false);
        goldText.text = expText.text = "x-";
        UITween.Blink(goldText);
        UITween.Blink(expText);
    }

    private static string FormatTime(int seconds)
        => $"{seconds / 60:00}:{seconds % 60:00}";

    private static string FormatAmount(int amount)
        => amount >= 10000 ? $"{amount / 1000f:0.#}K" : amount.ToString();
}
