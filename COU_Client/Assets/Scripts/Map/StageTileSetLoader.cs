using System.Collections.Generic;
using UnityEngine;

// stage_{n}_background.png(3×3 타일셋)를 불러와 9개의 스프라이트로 나눈다.
// 같은 스테이지는 한 번만 만들고 재사용한다.
public static class StageTileSetLoader
{
    public const int GridSize = 3;
    public const int TileCount = GridSize * GridSize;

    private static readonly Dictionary<string, Sprite[]> cache = new();

    // 도메인 리로드를 끈 설정에서도 Play를 시작할 때마다 캐시를 비운다
    // (이전 Play에서 만든 스프라이트는 Play 종료 시 파괴되기 때문)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        cache.Clear();
    }


    // pathFormat 예: "Backgrounds/stage_{0}_background" (Resources 기준, 확장자 제외)
    public static Sprite[] Load(string pathFormat, int stageNumber, int insetPixels, float tileWorldSize)
    {
        string path = string.Format(pathFormat, stageNumber);
        string key = path + "|" + insetPixels + "|" + tileWorldSize;

        if (cache.TryGetValue(key, out var cached))
        {
            // 파괴된 스프라이트가 남아 있으면 버리고 다시 만든다
            if (IsAlive(cached))
                return cached;

            cache.Remove(key);
        }

        var texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogError("[StageTileSetLoader] 배경 이미지를 찾을 수 없습니다: Resources/" + path);
            return null;
        }

        int cellWidth = texture.width / GridSize;
        int cellHeight = texture.height / GridSize;
        int tileWidth = cellWidth - insetPixels * 2;
        int tileHeight = cellHeight - insetPixels * 2;

        if (tileWidth <= 0 || tileHeight <= 0)
        {
            Debug.LogError("[StageTileSetLoader] insetPixels가 너무 큽니다: " + insetPixels);
            return null;
        }

        // 타일 가로 길이가 tileWorldSize 유닛이 되도록 PPU 계산
        float pixelsPerUnit = tileWidth / tileWorldSize;

        var sprites = new Sprite[TileCount];

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                // 텍스처 좌표는 왼쪽 아래가 (0, 0)이라, 이미지 위쪽 줄이 0번이 되도록 뒤집는다
                int x = col * cellWidth + insetPixels;
                int y = (GridSize - 1 - row) * cellHeight + insetPixels;

                var sprite = Sprite.Create(
                    texture,
                    new Rect(x, y, tileWidth, tileHeight),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);

                int index = row * GridSize + col;
                sprite.name = "stage_" + stageNumber + "_tile_" + index;
                sprites[index] = sprite;
            }
        }

        cache[key] = sprites;
        return sprites;
    }

    private static bool IsAlive(Sprite[] sprites)
    {
        foreach (var sprite in sprites)
        {
            if (sprite == null)   // Unity 오브젝트는 파괴되면 null과 같다고 판정됨
                return false;
        }
        return true;
    }
}