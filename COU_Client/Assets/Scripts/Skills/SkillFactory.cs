using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    private static readonly Dictionary<long, Func<SkillBase>> _creators = new()
    {
        // 액티브 (20000대)
        { GameConstants.TemporaryConstClass.TUMBLER, () => new Skill_Tumbler() },
        { GameConstants.TemporaryConstClass.GLASSES, () => new Skill_Glasses() },
        { GameConstants.TemporaryConstClass.MONITOR, () => new Skill_Monitor() },
        { GameConstants.TemporaryConstClass.SPEAKER, () => new Skill_Speaker() },
        { GameConstants.TemporaryConstClass.MOUSE, () => new Skill_Mouse() },
        { GameConstants.TemporaryConstClass.KEYBOARD, () => new Skill_Keyboard() },
        { GameConstants.TemporaryConstClass.CPU, () => new Skill_CPU() },
        { GameConstants.TemporaryConstClass.POWER_STRIP, () => new Skill_PowerStrip() },
        { GameConstants.TemporaryConstClass.CTRL_Z, () => new Skill_CtrlZ() },

        // 패시브 (21000대)
        { GameConstants.TemporaryConstClass.ENERGY_DRINK, () => new Skill_EnergyDrink() },
        { GameConstants.TemporaryConstClass.EYE_DROPS, () => new Skill_EyeDrops() },
        { GameConstants.TemporaryConstClass.SECOND_MONITOR, () => new Skill_SecondMonitor() },
        { GameConstants.TemporaryConstClass.EARBUDS, () => new Skill_Earbuds() },
        { GameConstants.TemporaryConstClass.MOUSE_PAD, () => new Skill_MousePad() },
        { GameConstants.TemporaryConstClass.KEYBOARD_SWITCH, () => new Skill_KeyboardSwitch() },
        { GameConstants.TemporaryConstClass.OVERCLOCK, () => new Skill_Overclock() },
        { GameConstants.TemporaryConstClass.STATIC_SHOCK, () => new Skill_StaticShock() },
        { GameConstants.TemporaryConstClass.SHIFT_KEY, () => new Skill_ShiftKey() },
    };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: 시트 값을 무시하고 여기서 스킬 수치를 직접 덮어씀. 쓰지 않을 땐 항목을 지우거나 주석 처리할 것
    // 수정 후 재생을 다시 시작하거나, 이미 장착한 스킬은 해제 후 다시 장착해야 반영됨
    //private static readonly Dictionary<long, Action<SkillData>> _debugOverrides = new()
    //{
    //    { GameConstants.TemporaryConstClass.KEYBOARD, data => {
    //        data.skillTargetType = "Nearest";
    //        data.skillCooldown = 0.75f;
    //        data.skillSpeed = 0.0f;
    //        data.skillDamage = 1.25f;
    //        data.skillRange = 1.0f;
    //    } },
    //    { GameConstants.TemporaryConstClass.TUMBLER, data => {
    //        data.skillTargetType ="Nearest";
    //        data.skillCooldown = 1.0f;
    //        data.skillSpeed = 4.0f;
    //        data.skillDamage = 1.0f;
    //        data.skillRange = 5.0f;
    //    } },
    //    { GameConstants.TemporaryConstClass.GLASSES, data => {
    //        data.skillTargetType ="None";
    //        data.skillCooldown = 1.0f;
    //        data.skillSpeed = 0.0f;
    //        data.skillDamage = 1.0f;
    //        data.skillRange = 3.0f;
    //    } },
    //    { GameConstants.TemporaryConstClass.MONITOR, data => {
    //        data.skillTargetType ="Nearest";
    //        data.skillCooldown = 2.0f;
    //        data.skillSpeed = 0.5f;
    //        data.skillDamage = 0.0f;
    //        data.skillRange = 3.0f;
    //    } },
    //};
#endif

    public static bool IsRegistered(long skillId) => _creators.ContainsKey(skillId);

    public static SkillBase Create(long skillId)
    {
        if(!_creators.TryGetValue(skillId, out Func<SkillBase> creator))
        {
            Debug.LogWarning($"[SkillFactory] 등록되지 않은 스킬 ID입니다: {skillId}");
            return null;
        }

        SkillData data = GameManager.JsonData.GetSkillDataFromJson(skillId);

        if(data == null)
        {
            Debug.LogWarning($"[SkillFactory] 스킬 데이터가 없습니다: {skillId}");
            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        //if(_debugOverrides.TryGetValue(skillId, out Action<SkillData> overrideAction))
        //{
        //    // 딕셔너리의 원본이 바뀌지 않도록 복사본에만 적용
        //    data = data.Clone();
        //    overrideAction(data);
        //    Debug.Log($"[SkillFactory] 디버그 덮어쓰기 적용: {skillId} (쿨타임 {data.skillCooldown}, 속도 {data.skillSpeed}, 대미지 {data.skillDamage}, 사거리 {data.skillRange})");
        //}
#endif

        SkillBase skill = creator();

        // 시트의 분류와 클래스가 어긋나면 슬롯/UI 분류가 틀어지므로 바로 알 수 있게 경고
        bool isPassiveClass = skill is PassiveSkillBase;

        if(isPassiveClass != (data.skillCategory == SkillCategory.Passive))
        {
            Debug.LogWarning($"[SkillFactory] 시트의 skillCategory와 스킬 클래스가 맞지 않습니다: {skillId} ({data.skillCategory})");
        }

        skill.Initialize(data);

        return skill;
    }
}