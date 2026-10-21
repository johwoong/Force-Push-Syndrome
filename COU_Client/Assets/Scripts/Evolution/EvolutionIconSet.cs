using UnityEngine;

// 진화 스탯 종류별 노드 아이콘
[CreateAssetMenu(menuName = "COU/Evolution/Evolution Icon Set", fileName = "EvolutionIconSet")]
public class EvolutionIconSet : ScriptableObject
{
    [SerializeField] private Sprite attack;
    [SerializeField] private Sprite hp;
    [SerializeField] private Sprite defense;
    [SerializeField] private Sprite potionRecovery;

    // 없는 스탯이면 null (노드 템플릿의 기본 아이콘 유지)
    public Sprite Get(string statType) => statType switch
    {
        PlayerStatType.Attack => attack,
        PlayerStatType.Hp => hp,
        PlayerStatType.Defense => defense,
        PlayerStatType.PotionRecovery => potionRecovery,
        _ => null
    };
}