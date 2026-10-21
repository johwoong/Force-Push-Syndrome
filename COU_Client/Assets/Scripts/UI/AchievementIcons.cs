using System.Collections.Generic;
using UnityEngine;

// 업적 아이콘. Resources/UI/Icon_Achievements(Sprite Mode: Multiple)의
// "Icon_Achievements_{업적ID}" 스프라이트를 처음 한 번만 읽어 둔다
public static class AchievementIcons
{
    private const string SheetPath = "UI/Icon_Achievements";
    private const string NamePrefix = "Icon_Achievements_";

    private static Dictionary<int, Sprite> sprites;

    public static Sprite Get(int achievementId)
    {
        if (sprites == null)
            Load();

        if (sprites != null && sprites.TryGetValue(achievementId, out Sprite sprite))
            return sprite;

        Debug.LogWarning($"[AchievementIcons] 아이콘이 없습니다: {NamePrefix}{achievementId}");
        return null;
    }

    private static void Load()
    {
        sprites = new Dictionary<int, Sprite>();

        foreach (Sprite sprite in Resources.LoadAll<Sprite>(SheetPath))
        {
            if (!sprite.name.StartsWith(NamePrefix)) continue;

            if (int.TryParse(sprite.name.Substring(NamePrefix.Length), out int id))
                sprites[id] = sprite;
        }
    }
}
