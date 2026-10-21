using UnityEngine;
using UnityEngine.UI;

public class ChallengeTab : UIView
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private ChallengeChapterView chapterTemplate;
    [SerializeField] private Button backButton;
    [SerializeField] private Button topButton;

    private void Awake()
    {
        backButton.onClick.AddListener(Close);
        topButton.onClick.AddListener(ScrollToTop);
    }

    public void SetChapters(ChallengeChapterInfo[] chapters)
    {
        foreach (var chapter in chapters)
        {
            var view = Instantiate(chapterTemplate, scrollRect.content);
            view.gameObject.SetActive(true);
            view.Set(chapter);
        }
    }

    private void ScrollToTop()
        => UITween.ScrollTo(scrollRect, 1f);
}
