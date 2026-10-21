using System.Text;
using UnityEditor;
using UnityEngine;

// Play 중 현재 장착 장비의 등급 효과 합을 로그로 확인 (에디터 전용)
public static class EquipmentEffectDebugMenu
{
    [MenuItem("COU/Debug/장착 장비 등급 효과 합 로그")]
    private static void LogEquipped()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[EquipmentEffectDebug] Play 중에만 실행할 수 있습니다.");
            return;
        }

        var dataManager = GameManager.PlayerData;
        if (dataManager == null || !dataManager.isPlayerDataLoaded)
        {
            Debug.LogWarning("[EquipmentEffectDebug] 로그인 데이터가 없습니다.");
            return;
        }

        var equipped = PlayerStatCalculator.GetEquipped(dataManager.currentData);

        var sb = new StringBuilder("[EquipmentEffectDebug] 장착 장비 ").Append(equipped.Count).Append("개\n");
        foreach (var item in equipped)
            sb.Append("  ").Append(item.itemId).Append(" ").Append(item.Data.itemName)
              .Append(" (").Append(item.Grade).Append(", Lv").Append(item.level).Append(")\n");

        sb.Append(EquipmentEffectResolver.Resolve(equipped));
        Debug.Log(sb.ToString());
    }
}