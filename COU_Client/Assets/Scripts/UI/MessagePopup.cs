using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePopup : UIPopup
{
    private const string DefaultTitle = "알림";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;

    private Action onConfirm;
    private Action onClose;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(Close);
        closeButton.onClick.AddListener(Close);
        dimButton.onClick.AddListener(Close);
    }

    public void ShowAlert(string message, Action onConfirm = null, string title = DefaultTitle)
    {
        Show(title, message, onConfirm, onConfirm, false);
    }

    public void ShowConfirm(string message, Action onConfirm, Action onCancel = null, string title = DefaultTitle)
    {
        Show(title, message, onConfirm, onCancel, true);
    }

    public override void Close()
    {
        base.Close();

        var callback = onClose;
        onClose = null;
        onConfirm = null;
        callback?.Invoke();
    }

    private void Show(string title, string message, Action confirm, Action close, bool showCancel)
    {
        titleText.text = title;
        messageText.text = message;
        onConfirm = confirm;
        onClose = close;
        cancelButton.gameObject.SetActive(showCancel);
        Open();
    }

    private void OnConfirmButtonClicked()
    {
        onClose = onConfirm;
        Close();
    }
}
