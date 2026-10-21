using System.Collections.Generic;
using UnityEngine;

// 배경 타일(SpriteRenderer)을 만들고, 쓰지 않는 타일은 꺼 두었다가 재사용한다.
public class BackgroundTilePool
{
    private readonly Transform parent;
    private readonly string sortingLayerName;
    private readonly int sortingOrder;
    private readonly Stack<SpriteRenderer> inactive = new();

    public BackgroundTilePool(Transform parent, string sortingLayerName, int sortingOrder)
    {
        this.parent = parent;
        this.sortingLayerName = sortingLayerName;
        this.sortingOrder = sortingOrder;
    }

    public SpriteRenderer Get()
    {
        var tile = inactive.Count > 0 ? inactive.Pop() : Create();
        tile.gameObject.SetActive(true);
        return tile;
    }

    public void Release(SpriteRenderer tile)
    {
        tile.gameObject.SetActive(false);
        inactive.Push(tile);
    }

    private SpriteRenderer Create()
    {
        var go = new GameObject("BackgroundTile");
        go.transform.SetParent(parent, false);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}