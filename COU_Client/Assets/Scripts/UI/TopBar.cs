using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBar : UIView
{
    private const int ShopTabIndex = 0;

    [SerializeField] private Button profileButton;
    [SerializeField] private Button staminaPlusButton;
    [SerializeField] private Button gemPlusButton;
    [SerializeField] private Button goldPlusButton;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expFill;
    [SerializeField] private GameObject staminaGroup;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private GameObject coinGroup;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private TMP_Text goldText;

    // 재화는 바뀐 만큼 굴려서 보여준다. -1은 아직 표시한 적 없다는 뜻 (처음에는 바로 표시)
    private int stamina = -1;
    private int coin = -1;
    private int gem = -1;
    private int gold = -1;

    private void Awake()
    {
        profileButton.onClick.AddListener(() => UIManager.Instance.Open<ProfilePopup>());
        staminaPlusButton.onClick.AddListener(OpenShop);
        gemPlusButton.onClick.AddListener(OpenShop);
        goldPlusButton.onClick.AddListener(OpenShop);
    }

    private void OpenShop()
        => UIManager.Instance.Get<TabBar>().Select(ShopTabIndex);

    public void SetNickname(string nickname)
        => nicknameText.text = nickname;

    public void SetLevel(int level)
    {
        var text = level.ToString();
        if (levelText.text == text) return;

        levelText.text = text;
        UITween.Punch(levelText.transform, 0.3f);
    }

    public void SetExp(float ratio)
        => UITween.Fill(expFill, ratio, true);

    public void SetStamina(int current, int max)
    {
        UITween.Count(staminaText, stamina, current, value => $"{value}/{max}");
        stamina = current;
    }

    public void SetCoin(int coin)
    {
        UITween.Count(coinText, this.coin, coin, FormatAmount);
        this.coin = coin;
    }

    public void SetGem(int gem)
    {
        UITween.Count(gemText, this.gem, gem, FormatAmount);
        this.gem = gem;
    }

    public void SetGold(int gold)
    {
        UITween.Count(goldText, this.gold, gold, FormatAmount);
        this.gold = gold;
    }

    public void ShowCoin(bool show)
    {
        var changed = coinGroup.activeSelf != show;
        staminaGroup.SetActive(!show);
        coinGroup.SetActive(show);

        if (changed)
            UITween.PopIn((show ? coinGroup : staminaGroup).transform);
    }

    private static readonly string[] Units = { "K", "M", "B" };

    private static string FormatAmount(int amount)
    {
        if (amount < 10000) return amount.ToString();

        int divisor = 1000;
        int unit = 0;
        while (amount / divisor >= 1000 && unit < Units.Length - 1)
        {
            divisor *= 1000;
            unit++;
        }
        return $"{amount / (divisor / 10) / 10f:0.#}{Units[unit]}";
    }
}
