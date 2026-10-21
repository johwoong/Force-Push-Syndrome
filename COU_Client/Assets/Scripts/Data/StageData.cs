using System;
using System.Linq;
using UnityEngine;

// 스테이지 관련 데이터
[Serializable]
public class StageData
{
    public int stageId;
    public string stageName;
    public string stageIllustrationColor;
    public int waveId;            // Wave.json의 웨이브. 여러 스테이지가 같은 웨이브를 쓸 수 있음
    public string stageDescription;
    public int clearAccountExp;
    public int[] rewardBoxGradeWeights;   // 보상상자 장비 등급 가중치. ItemGrade 순서(General, Super, Rare)

    [NonSerialized] private Color parsedColor;
    public Color IllustrationColor => parsedColor;

    [NonSerialized] private Sprite icon;
    public Sprite Icon => icon;   // Resources/Backgrounds/StageIcon/stage_{stageId}

    public void OnLoaded()
    {
        icon = Resources.Load<Sprite>($"Backgrounds/StageIcon/stage_{stageId}");

        if (!ColorUtility.TryParseHtmlString(stageIllustrationColor, out parsedColor))
        {
            Debug.LogWarning($"[StageData] {stageId}의 stageIllustrationColor \"{stageIllustrationColor}\"을(를) 파싱할 수 없어 회색으로 처리합니다.");
            parsedColor = Color.gray;
        }
    }

    // 보상상자 하나의 장비 등급을 가중치로 뽑는다
    public ItemGrade RollRewardBoxGrade()
    {
        int roll = UnityEngine.Random.Range(0, rewardBoxGradeWeights.Sum());
        for (int i = 0; i < rewardBoxGradeWeights.Length; i++)
        {
            roll -= rewardBoxGradeWeights[i];
            if (roll < 0) return (ItemGrade)i;
        }
        return ItemGrade.General;
    }
}
