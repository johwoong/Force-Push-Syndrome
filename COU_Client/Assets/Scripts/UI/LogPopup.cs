using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogPopup : UIPopup
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        dimButton.onClick.AddListener(Close);
    }

    public void Show(string title, string body)
    {
        titleText.text = title;
        bodyText.text = body;
        Open();
    }
}
