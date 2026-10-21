using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySortButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private InventoryListView listView;

    private void Awake()
    {
        button.onClick.AddListener(OnClicked);
        UpdateLabel(listView.CurrentSortMode);
    }

    private void OnClicked()
    {
        var mode = listView.CycleSortMode();
        UpdateLabel(mode);
    }

    private void UpdateLabel(InventoryListView.SortMode mode)
    {
        label.text = mode switch
        {
            InventoryListView.SortMode.Slot => "슬롯",
            InventoryListView.SortMode.Level => "레벨",
            InventoryListView.SortMode.Grade => "등급",
            _ => label.text
        };
    }
}
