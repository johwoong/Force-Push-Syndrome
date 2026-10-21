using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 목록의 한 줄 (4위 이하)
public class RankingEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private GameObject mineOverlay;
    [SerializeField] private Button friendButton;

    private float nameDefaultMinY;

    private void Awake()
        => nameDefaultMinY = nameText.rectTransform.anchorMin.y;

    public void Setup(RankingEntryResponse entry, string valueString, bool showLevel, bool isMine, Action<RankingEntryResponse> onAddFriend)
    {
        rankText.text = entry.rank.ToString();
        nameText.text = entry.nickname;
        valueText.text = valueString;
        mineOverlay.SetActive(isMine);

        // 내 줄에는 친구 추가 버튼을 숨김
        friendButton.gameObject.SetActive(!isMine);
        friendButton.onClick.RemoveAllListeners();
        friendButton.onClick.AddListener(() => onAddFriend?.Invoke(entry));

        // 레벨을 안 보여줄 땐 이름을 줄 가운데로
        levelText.gameObject.SetActive(showLevel);
        levelText.text = $"Lv.{entry.accountLevel}";

        var min = nameText.rectTransform.anchorMin;
        min.y = showLevel ? nameDefaultMinY : 0f;
        nameText.rectTransform.anchorMin = min;
    }
}
