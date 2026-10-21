using System;
using UnityEngine;

[Serializable]
public struct EvolutionInfo
{
    public int accountLevel;
    public int gold;
    public int dna;
    public EvolutionNodeInfo[] goldNodes;
    public EvolutionNodeInfo[] dnaNodes;
    public int unlockedGoldNodes;
    public int unlockedDnaNodes;
}

[Serializable]
public struct EvolutionNodeInfo
{
    public int level;
    public string name;
    public string value;
    public string description;
    public Sprite icon;
    public Color iconColor;
    public int cost;
}
