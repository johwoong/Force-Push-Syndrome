using UnityEngine;
using UnityEngine.UI;

public class ProfilePopup : UIPopup
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button rankingButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        settingsButton.onClick.AddListener(() => UIManager.Instance.Open<SettingsPopup>());
        rankingButton.onClick.AddListener(() => UIManager.Instance.Open<RankingPopup>());
    }
}
