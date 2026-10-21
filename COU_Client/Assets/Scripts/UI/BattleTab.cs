using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleTab : UIView
{
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private Image illustration;
    [SerializeField] private Button illustrationButton;
    [SerializeField] private TMP_Text staminaCostText;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button friendButton;
    [SerializeField] private Button rankingButton;
    [SerializeField] private Button chapterChestButton;
    [SerializeField] private GameObject chapterChestLock;
    [SerializeField] private Button patrolButton;
    [SerializeField] private GameObject patrolLock;

    private int currentStageId;
    private int staminaCost;
    private bool isRequesting;
    private int claimableChestStageId;   // 받을 수 있는 챕터 보물상자의 스테이지. 0이면 없음
    private bool canPatrol;

    private void Awake()
    {
        illustrationButton.onClick.AddListener(() => UIManager.Instance.Open<StageSelectScreen>());
        gameStartButton.onClick.AddListener(StartBattle);
        settingsButton.onClick.AddListener(() => UIManager.Instance.Open<SettingsPopup>());
        friendButton.onClick.AddListener(() => UIManager.Instance.Open<FriendPopup>());
        rankingButton.onClick.AddListener(() => UIManager.Instance.Open<RankingPopup>());
        chapterChestButton.onClick.AddListener(ClaimChapterChest);
        patrolButton.onClick.AddListener(ConfirmPatrol);
    }

    // 다른 스테이지로 바뀌면 이름이 튀어나온다 (처음 설정할 때는 바로 적용)
    public void SetStage(StageData stage, int bestTime)
    {
        var changed = currentStageId != 0 && currentStageId != stage.stageId;

        currentStageId = stage.stageId;
        stageNameText.text = stage.stageName;
        bestTimeText.text = bestTime > 0
            ? $"최장 생존시간: {bestTime / 60:00}:{bestTime % 60:00}"
            : "최장 생존시간:";
        illustration.sprite = stage.Icon;

        if (changed)
            UITween.PopIn(stageNameText.transform);
    }

    public void SetStaminaCost(int cost)
    {
        staminaCost = cost;
        staminaCostText.text = $"x {cost}";
    }

    public void SetChapterChest(int claimableStageId)
    {
        claimableChestStageId = claimableStageId;
        SetLocked(chapterChestButton, chapterChestLock, claimableStageId == 0);
    }

    public void SetPatrolUnlocked(bool unlocked)
    {
        canPatrol = unlocked;
        SetLocked(patrolButton, patrolLock, !unlocked);
    }

    // 잠기면 자물쇠가 보이고 버튼이 흐려진다
    private static void SetLocked(Button button, GameObject lockIcon, bool locked)
    {
        lockIcon.SetActive(locked);
        button.GetComponent<CanvasGroup>().alpha = locked ? 0.5f : 1f;
    }

    // 클리어했지만 아직 받지 않은 가장 앞 스테이지의 보물상자를 받는다
    private void ClaimChapterChest()
    {
        if (isRequesting) return;
        if (claimableChestStageId == 0)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", "받을 수 있는 챕터 보물상자가 없습니다.\n스테이지를 클리어하면 열립니다.");
            return;
        }

        isRequesting = true;
        StartCoroutine(ChapterChestApi.Claim(claimableChestStageId, result =>
        {
            isRequesting = false;

            if (!result.IsSuccess)
            {
                UIManager.Instance.Get<LogPopup>().Show("알림", result.ToUserMessage("챕터 보물상자를 받지 못했습니다."));
                return;
            }

            // 장비 추가가 NotifyPlayerDataChanged로 버튼 상태를 다시 설정하므로 받은 기록을 먼저 넣는다
            GameManager.PlayerData.currentData.claimedChapterChests.Add(result.Data.stageId);
            var added = PlayerInventory.Instance.AddServerItems(result.Data.rewardedItems);
            UIManager.Instance.Get<RewardBoxResultPopup>().Show(added);
        }));
    }

    private void ConfirmPatrol()
    {
        if (isRequesting) return;
        if (!canPatrol)
        {
            UIManager.Instance.Get<LogPopup>().Show("알림", "스테이지를 클리어하면 바이브 코딩할 수 있습니다.");
            return;
        }

        UIManager.Instance.Get<MessagePopup>().ShowConfirm(
            $"스태미나 {GameManager.JsonData.AccountConstData.patrolStaminaCost}를 소모해 바이브 코딩할까요?\n최고 클리어 스테이지를 한 번 클리어한 만큼 보상을 받습니다.",
            Patrol, title: "바이브 코딩");
    }

    // 서버가 스태미나를 차감하고 최고 클리어 스테이지의 평균 보상을 지급한다
    private void Patrol()
    {
        if (isRequesting) return;
        isRequesting = true;

        StartCoroutine(BattleApi.Patrol(result =>
        {
            isRequesting = false;

            if (!result.IsSuccess)
            {
                UIManager.Instance.Get<LogPopup>().Show("알림", result.ToUserMessage("바이브 코딩하지 못했습니다."));
                return;
            }

            // profile은 장착 칸을 유지하려고 레벨과 경험치만 옮긴다
            var response = result.Data;
            var data = GameManager.PlayerData.currentData;
            data.profile.accountLevel = response.profile.accountLevel;
            data.profile.accountExp = response.profile.accountExp;
            data.currency = response.currency;
            var added = PlayerInventory.Instance.AddServerItems(response.rewards);

            UIManager.Instance.Get<LogPopup>().Show("바이브 코딩 완료",
                $"골드 +{response.grantedGold}\n경험치 +{response.grantedExp}\n장비 {added.Count}개");
            if (added.Count > 0)
                UIManager.Instance.Get<RewardBoxResultPopup>().Show(added);
        }));
    }

    // 서버가 해금과 스태미나를 검증해 차감하고 battleId를 발급한다
    public void StartBattle()
    {
        if (isRequesting) return;
        isRequesting = true;
        UITween.Blink(gameStartButton.targetGraphic, 0.5f);   // 응답을 기다리는 동안 버튼이 깜빡인다

        // 응답의 재화 반영(NotifyPlayerDataChanged)이 선택 스테이지를 다시 설정하므로 요청한 스테이지를 잡아 둔다
        int stageId = currentStageId;

        StartCoroutine(BattleApi.Enter(stageId, result =>
        {
            isRequesting = false;
            UITween.StopBlink(gameStartButton.targetGraphic);

            if (!result.IsSuccess)
            {
                UIManager.Instance.Get<LogPopup>().Show("알림", GetEnterErrorMessage(result));
                return;
            }

            // 차감 후 재화로 덮어쓴다
            GameManager.PlayerData.currentData.currency = result.Data.currency;
            GameManager.PlayerData.NotifyPlayerDataChanged();

            GameManager.Scene.ChangeSceneWithLoading(GameConstants.SceneNames.BATTLE_SCENE, stageId, result.Data.battleId);
        }));
    }

    private static string GetEnterErrorMessage(ApiResult<BattleEnterResponse> result)
    {
        if (result.FailType == ApiFailType.NetworkError)
            return "서버에 연결할 수 없습니다. 잠시 후 다시 시도해 주세요.";

        return result.Error?.code switch
        {
            BattleApi.ErrorStageLocked => "아직 열리지 않은 스테이지입니다.",
            BattleApi.ErrorNotEnoughStamina => "스태미나가 부족합니다.",
            _ => string.IsNullOrEmpty(result.Error?.message) ? "전투에 입장하지 못했습니다." : result.Error.message
        };
    }
}
