using System;
using UnityEngine;

[Serializable]
public struct ChallengeChapterInfo
{
    public int number;
    public StageData stage;
    public ChallengeInfo[] challenges;
}

[Serializable]
public struct ChallengeInfo
{
    public ChallengeCondition[] conditions;
    public ChallengeReward[] rewards;
    public bool rewarded;
}

[Serializable]
public struct ChallengeCondition
{
    public string name;
    public string description;
}

[Serializable]
public struct ChallengeReward
{
    public ItemGrade grade;
    public Color iconColor;
    public int amount;
    public bool techPart;
}
