using UnityEngine;
using UnityEngine.UI;

public class HomePopup : UIPopup
{
    [SerializeField] private Button exitButton;
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        exitButton.onClick.AddListener(Exit);
        continueButton.onClick.AddListener(Close);
    }

    private void Exit()
    {
        Close();
        UIManager.Instance.Close<PauseWindow>();
        BattleManager.Instance.Surrender();
    }
}
