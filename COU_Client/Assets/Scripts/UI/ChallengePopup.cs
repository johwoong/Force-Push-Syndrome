using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengePopup : UIPopup
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text conditionTemplate;
    [SerializeField] private RewardSlot rewardTemplate;
    [SerializeField] private GameObject techPartTemplate;

    private readonly List<GameObject> items = new();

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
    }

    public void Set(int chapterNumber, ChallengeInfo challenge)
    {
        titleText.text = $"{chapterNumber} 챕터";

        foreach (var item in items)
            Destroy(item);
        items.Clear();

        foreach (var condition in challenge.conditions)
        {
            var row = Instantiate(conditionTemplate, conditionTemplate.transform.parent);
            row.text = $"<color=#8FE605>{condition.name}</color> : {condition.description}";
            Add(row.gameObject);
        }

        foreach (var reward in challenge.rewards)
        {
            if (reward.techPart)
            {
                Add(Instantiate(techPartTemplate, techPartTemplate.transform.parent));
                continue;
            }

            var slot = Instantiate(rewardTemplate, rewardTemplate.transform.parent);
            slot.Set(reward);
            Add(slot.gameObject);
        }
    }

    private void Add(GameObject item)
    {
        item.SetActive(true);
        items.Add(item);
    }
}
