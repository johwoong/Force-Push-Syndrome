using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 채팅 말풍선 
public class ChatMessageView : MonoBehaviour
{
    private static readonly Color MineColor = new Color(0.2f, 0.6039216f, 0.9411765f);
    private static readonly Color OtherColor = new Color(0.21568628f, 0.2627451f, 0.33333334f);

    private const float MaxTextWidth = 620f;  

    [SerializeField] private Image bubble;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timeText;

    public void Setup(ChatMessageResponse message)
    {
        messageText.text = message.content;
        float oneLineWidth = messageText.GetPreferredValues(message.content).x;
        messageText.GetComponent<LayoutElement>().preferredWidth = Mathf.Min(Mathf.Ceil(oneLineWidth), MaxTextWidth);

        bool mine = message.mine;
        bubble.color = mine ? MineColor : OtherColor;

        GetComponent<HorizontalLayoutGroup>().childAlignment = mine ? TextAnchor.LowerRight : TextAnchor.LowerLeft;
        timeText.transform.SetSiblingIndex(mine ? 0 : 1);
        timeText.text = message.createdAt.ToLocalTime().ToString("HH:mm");
    }
}
