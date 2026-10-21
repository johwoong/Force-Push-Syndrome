using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LuckTrainWindow : UIView
{
    private enum State
    {
        Idle,
        Spin,
        Reveal,
        Result
    }

    [SerializeField] private Image[] slotIcons;
    [SerializeField] private GameObject[] slotHighlights;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject skipHint;
    [SerializeField] private Button skipArea;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private LuckTrainRewardCard[] rewardCards;
    [SerializeField] private Button continueButton;
    [SerializeField] private float spinDuration = 2.5f;
    [SerializeField] private int spinLaps = 2;
    [SerializeField] private float revealInterval = 0.3f;
    [SerializeField][Range(0f, 1f)] private float coinVolume = 0.5f;   // 코인 효과음 음량 배율
    [SerializeField][Range(0f, 1f)] private float resultVolume = 0.5f;   // 결과 칸 효과음 음량 배율

    public event Action OnFinished;

    private State state;
    private int[] selected;
    private int count;
    private int gold;
    private int totalSteps;
    private int highlighted;
    private int revealed;
    private float timer;

    private void Awake()
    {
        startButton.onClick.AddListener(StartSpin);
        skipArea.onClick.AddListener(ShowResult);
        continueButton.onClick.AddListener(Finish);
    }

    private void Update()
    {
        if (state == State.Spin)
            UpdateSpin();
        else if (state == State.Reveal)
            UpdateReveal();
    }

    public void SetSlot(int index, Sprite icon)
        => slotIcons[index].sprite = icon;

    public void SetReward(int index, Sprite icon, string name, int grade, string description)
        => rewardCards[index].Set(icon, name, grade, description);

    public void Show(int[] selected, int count, int gold)
    {
        this.selected = selected;
        this.count = count;
        this.gold = gold;
        state = State.Idle;

        foreach (var highlight in slotHighlights)
            highlight.SetActive(false);
        goldText.text = "0";
        startButton.gameObject.SetActive(true);
        skipHint.SetActive(false);
        skipArea.gameObject.SetActive(false);
        resultPanel.SetActive(false);
        for (var i = 0; i < rewardCards.Length; i++)
            rewardCards[i].gameObject.SetActive(i < count);
        GameManager.Sound.PreloadSFX(GameConstants.Sfx.LUCK_TRAIN_TICK, GameConstants.Sfx.LUCK_TRAIN_RESULT);
        Open();
    }

    private void StartSpin()
    {
        startButton.gameObject.SetActive(false);
        skipHint.SetActive(true);
        skipArea.gameObject.SetActive(true);

        totalSteps = spinLaps * slotHighlights.Length + selected[0];
        highlighted = 0;
        slotHighlights[highlighted].SetActive(true);
        PlayCoin();
        timer = 0f;
        state = State.Spin;
    }

    private void UpdateSpin()
    {
        timer += Time.unscaledDeltaTime;
        var t = Mathf.Min(timer / spinDuration, 1f);
        var progress = 1f - (1f - t) * (1f - t);

        var index = Mathf.FloorToInt(totalSteps * progress) % slotHighlights.Length;
        if (index != highlighted)
        {
            slotHighlights[highlighted].SetActive(false);
            slotHighlights[index].SetActive(true);
            highlighted = index;
            UITween.Punch(slotIcons[index].transform, 0.12f);
            PlayCoin();
        }
        goldText.text = Mathf.FloorToInt(gold * progress).ToString();

        if (t < 1f) return;
        UITween.Punch(slotIcons[highlighted].transform, 0.35f);
        PlayResult();
        revealed = 1;
        timer = 0f;
        state = State.Reveal;
    }

    private void UpdateReveal()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < revealInterval) return;
        timer = 0f;

        if (revealed < count)
        {
            var slot = selected[revealed++];
            slotHighlights[slot].SetActive(true);
            UITween.PopIn(slotHighlights[slot].transform, 0f, 0.5f);
            UITween.Punch(slotIcons[slot].transform, 0.35f);
            PlayResult();
        }
        else
            ShowResult();
    }

    private void ShowResult()
    {
        foreach (var highlight in slotHighlights)
            highlight.SetActive(false);
        for (var i = 0; i < count; i++)
            slotHighlights[selected[i]].SetActive(true);
        goldText.text = gold.ToString();

        skipArea.gameObject.SetActive(false);
        resultPanel.SetActive(true);
        UITween.Stagger(rewardCards, 0.1f);
        state = State.Result;
    }

    // 칸에 들어갈 때 코인 효과음 (음량은 coinVolume × 설정창 효과음 볼륨)
    private void PlayCoin()
        => GameManager.Sound.PlaySFX(GameConstants.Sfx.LUCK_TRAIN_TICK, coinVolume);

    // 당첨 칸이 결정될 때 결과 효과음
    private void PlayResult()
        => GameManager.Sound.PlaySFX(GameConstants.Sfx.LUCK_TRAIN_RESULT, resultVolume);


    private void Finish()
    {
        Close();
        OnFinished?.Invoke();
    }
}
