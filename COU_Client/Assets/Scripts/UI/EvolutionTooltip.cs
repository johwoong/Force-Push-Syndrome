using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Image costIcon;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text unlockTitleText;

    private string defaultTitle;

    public Button UnlockButton => unlockButton;

    public void Show(EvolutionNodeInfo node, Vector2 position, bool unlockable, Color currencyColor)
        => Show(node, position, unlockable, unlockable, null, currencyColor);

    public void Show(EvolutionNodeInfo node, Vector2 position, bool showButton, bool interactable,
                     string buttonLabel, Color currencyColor)
    {
        if (unlockTitleText != null && defaultTitle == null)
            defaultTitle = unlockTitleText.text;

        nameText.text = node.name;
        valueText.text = node.value;
        descriptionText.text = node.description;

        unlockButton.gameObject.SetActive(showButton);
        unlockButton.interactable = interactable;
        if (unlockTitleText != null)
            unlockTitleText.text = string.IsNullOrEmpty(buttonLabel) ? defaultTitle : buttonLabel;

        costIcon.color = currencyColor;
        costText.text = $"x {node.cost}";
        ((RectTransform)transform).anchoredPosition = position;
        gameObject.SetActive(true);
    }

    public void Hide()
        => gameObject.SetActive(false);
}
