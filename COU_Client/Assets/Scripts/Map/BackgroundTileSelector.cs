using System.Collections.Generic;
using UnityEngine;

// 칸(셀)마다 어떤 타일을 놓을지 정한다.
// 주변 8칸에 이미 놓인 타일은 피하고, 한 번 정한 칸은 기억해서 다시 와도 같은 타일을 쓴다.
public class BackgroundTileSelector
{
    private static readonly Vector2Int[] Neighbors8 =
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0),             new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1),
    };

    private readonly Dictionary<Vector2Int, int> assigned = new();
    private readonly float[] weights;
    private readonly System.Random random;
    private readonly List<int> candidates = new();

    public BackgroundTileSelector(float[] weights, int seed)
    {
        this.weights = weights;
        random = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    public int GetTileIndex(Vector2Int cell)
    {
        if (assigned.TryGetValue(cell, out int index))
            return index;

        index = Pick(cell);
        assigned[cell] = index;
        return index;
    }

    public void Clear() => assigned.Clear();

    private int Pick(Vector2Int cell)
    {
        candidates.Clear();

        for (int i = 0; i < StageTileSetLoader.TileCount; i++)
        {
            if (GetWeight(i) <= 0f) continue;
            if (IsUsedByNeighbor(cell, i)) continue;
            candidates.Add(i);
        }

        // 가중치 0 설정 등으로 후보가 없으면 이웃 조건을 풀고 다시 고른다
        if (candidates.Count == 0)
        {
            for (int i = 0; i < StageTileSetLoader.TileCount; i++)
            {
                if (GetWeight(i) > 0f)
                    candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
            return 0;

        float total = 0f;
        foreach (int i in candidates)
            total += GetWeight(i);

        double roll = random.NextDouble() * total;
        foreach (int i in candidates)
        {
            roll -= GetWeight(i);
            if (roll <= 0) return i;
        }

        return candidates[candidates.Count - 1];
    }

    private bool IsUsedByNeighbor(Vector2Int cell, int tileIndex)
    {
        foreach (var offset in Neighbors8)
        {
            if (assigned.TryGetValue(cell + offset, out int neighbor) && neighbor == tileIndex)
                return true;
        }
        return false;
    }

    private float GetWeight(int index)
    {
        if (weights == null || index >= weights.Length) return 1f;
        return weights[index];
    }
}