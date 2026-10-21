using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeChapterView : MonoBehaviour
{
    [Serializable]
    private class Card
    {
        public Button button;
        public Image illustration;
        public GameObject rewarded;
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Card[] cards;

    private ChallengeChapterInfo chapter;

    private void Awake()
    {
        for (var i = 0; i < cards.Length; i++)
        {
            var index = i;
            cards[i].button.onClick.AddListener(() => OpenPopup(index));
        }
    }

    public void Set(ChallengeChapterInfo chapter)
    {
        this.chapter = chapter;
        titleText.text = chapter.stage.stageName;
        for (var i = 0; i < cards.Length; i++)
        {
            cards[i].illustration.color = chapter.stage.IllustrationColor;
            cards[i].rewarded.SetActive(chapter.challenges[i].rewarded);
        }
    }

    private void OpenPopup(int index)
    {
        var popup = UIManager.Instance.Get<ChallengePopup>();
        popup.Set(chapter.number, chapter.challenges[index]);
        popup.Open();
    }
}
