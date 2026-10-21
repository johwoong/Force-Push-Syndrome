using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상위 3명 카드
public class RankingPodiumView : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button friendButton;

    public void Setup(RankingEntryResponse entry, string valueString, bool isMine, Action<RankingEntryResponse> onAddFriend)
    {
        gameObject.SetActive(true);
        rankText.text = entry.rank.ToString();
        nameText.text = entry.nickname;
        valueText.text = valueString;

        // 내 카드에는 친구 추가 버튼을 숨김
        friendButton.gameObject.SetActive(!isMine);
        friendButton.onClick.RemoveAllListeners();
        friendButton.onClick.AddListener(() => onAddFriend?.Invoke(entry));
    }

    public void Clear()
        => gameObject.SetActive(false);
}
