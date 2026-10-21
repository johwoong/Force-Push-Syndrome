using UnityEngine;

public readonly struct SkillContext
{
    public readonly Transform Owner;
    public readonly PlayerMovement Movement;
    public readonly PlayerStats Stats;
    public readonly SkillModifiers Modifiers;

    public SkillContext(Transform owner, PlayerMovement movement, PlayerStats stats, SkillModifiers modifiers)
    {
        Owner = owner;
        Movement = movement;
        Stats = stats;
        Modifiers = modifiers;
    }
}