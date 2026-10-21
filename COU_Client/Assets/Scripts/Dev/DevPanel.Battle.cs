#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// 배틀 섹션: 배속, 레벨업, 아이템 효과, 골드·킬, 체력, 스킬
public partial class DevPanel
{
    private static readonly float[] Speeds = { 1f, 2f, 4f, 8f };

    private static bool CanDrawBattle => BattleManager.Instance != null && PlayerManager.Instance != null;

    private void DrawBattle()
    {
        var battle = BattleManager.Instance;
        var player = PlayerManager.Instance;

        GUILayout.Label("[전투]");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"배속 {battle.Speed}x");
        foreach (var speed in Speeds)
        {
            if (GUILayout.Button($"{speed}x"))
            {
                battle.SetSpeed(speed);
                message = $"배속 {speed}x";
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("레벨업"))
        {
            battle.LevelUp();
            message = "레벨업";
        }

        // 실제 획득과 같은 경로. OnLooted로 넘어가지 않아 업적 pickups에 세지 않는다
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("자석"))
        {
            player.Loot.OnLoot(DropItemType.Magnet);
            message = "자석";
        }
        if (GUILayout.Button("폭탄"))
        {
            player.Loot.OnLoot(DropItemType.Bomb);
            message = "폭탄";
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("골드 +1000"))
        {
            battle.AddGold(1000);
            message = "골드 +1000";
        }
        if (GUILayout.Button("킬 +100"))
        {
            for (var i = 0; i < 100; i++)
                battle.AddKill();
            message = "킬 +100";
        }
        GUILayout.EndHorizontal();

        // 피격과 같은 경로라 목숨을 잃은 직후의 무적 시간에는 무시된다
        if (GUILayout.Button("체력 -10%"))
        {
            var health = player.Health;
            health.GetDamage(Mathf.Max(1, Mathf.RoundToInt(health.MaxHealth * 0.1f)));
            message = $"체력 {health.CurrentHealth}/{health.MaxHealth}";
        }

        DrawSkills(player.Skills);
    }

    // 등록 스킬마다 장착/레벨업 버튼. BattleManager.ShowSelection과 같은 기준으로 나열하고, 업적 skillUpgrades에는 세지 않는다
    private void DrawSkills(SkillController skills)
    {
        GUILayout.Label("스킬");

        foreach (var data in GameManager.JsonData.SkillDataDic.Values)
        {
            if (!SkillFactory.IsRegistered(data.skillId)) continue;

            var skill = FindSkill(skills, data.skillId);

            GUILayout.BeginHorizontal();
            GUILayout.Label(skill == null ? data.skillName : $"{data.skillName} Lv{skill.Level}");

            GUI.enabled = skill == null || skill.Level < SkillBase.MaxLevel;
            if (GUILayout.Button(skill == null ? "장착" : "레벨업", GUILayout.Width(80f)))
            {
                if (skill == null)
                    message = skills.EquipSkill(data.skillId) ? $"{data.skillName} 장착" : $"{data.skillName} 장착 실패 (슬롯 가득 참)";
                else
                    message = skills.LevelUpSkill(data.skillId) ? $"{data.skillName} 레벨업" : $"{data.skillName} 레벨업 실패";
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
    }

    private static SkillBase FindSkill(SkillController skills, long skillId)
    {
        foreach (var skill in skills.ActiveSkills)
        {
            if (skill.SkillId == skillId) return skill;
        }
        foreach (var skill in skills.PassiveSkills)
        {
            if (skill.SkillId == skillId) return skill;
        }
        return null;
    }
}
#endif
