// 발동 없이 장착하는 동안 효과(스탯 보너스 등)만 적용하는 스킬의 베이스
// 보너스는 modifiers에 자기 자신(this)을 source로 등록하고, 해제는 RemoveSource(this)로 통째로 지움
public abstract class PassiveSkillBase : SkillBase
{
    public override void Tick(float deltaTime)
    {
    }

    protected override void OnEquip()
    {
        ApplyEffect();
    }

    protected override void OnUnequip()
    {
        RemoveEffect();
    }

    // 레벨에 따라 효과량이 달라지므로 기존 효과를 지우고 현재 레벨 기준으로 다시 적용
    // RemoveEffect는 레벨과 무관하게 source 단위로 지우기 때문에 이미 올라간 level을 읽어도 안전함
    protected override void OnLevelChanged()
    {
        RemoveEffect();
        ApplyEffect();
    }

    protected abstract void ApplyEffect();

    protected virtual void RemoveEffect()
    {
        modifiers.RemoveSource(this);
    }
}
