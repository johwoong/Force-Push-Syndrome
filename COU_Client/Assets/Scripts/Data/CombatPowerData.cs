using System;

[Serializable]
public class CombatPowerData
{
    public int combatPowerAttackWeight;
    public int combatPowerHpWeight;
    public int combatPowerDefenseWeight;
    public bool combatPowerUseCritical;   // 지금은 false (치명타 미사용)

    public void Validate()
    {
        if (combatPowerAttackWeight < 0 || combatPowerHpWeight < 0 || combatPowerDefenseWeight < 0)
            throw new InvalidOperationException("CombatPower의 가중치는 0 이상이어야 합니다.");
    }
}