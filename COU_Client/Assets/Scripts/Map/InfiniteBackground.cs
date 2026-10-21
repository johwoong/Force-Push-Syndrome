using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변 일정 범위에만 배경 타일을 깔고, 이동에 따라 생성·회수한다.
// WaveManager.Start에서 스테이지가 정해진 뒤에 실행되도록 실행 순서를 늦춘다.
[DefaultExecutionOrder(100)]
public class InfiniteBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;   // 비어 있으면 Fallback Stage Number 사용
    [SerializeField] private Transform target;          // 비어 있으면 PlayerManager에서 찾음

    [Header("Tile Set")]
    [SerializeField] private string resourcePathFormat = "Backgrounds/stage_{0}_background";
    [SerializeField][Min(1)] private int fallbackStageNumber = 1;
    [SerializeField][Min(0)] private int insetPixels = 6;          // 칸 사이 구분선을 잘라낼 픽셀 수

    [Header("Layout")]
    [SerializeField][Min(0.1f)] private float tileWorldSize = 8f;  // 타일 한 칸의 월드 크기(유닛)
    [SerializeField][Min(1)] private int activeRadius = 2;         // 플레이어 칸 기준 반경 (2 → 5×5)
    [SerializeField][Min(0)] private int releaseMargin = 1;        // 반경 밖으로 이만큼 더 멀어지면 회수
    [SerializeField][Min(0f)] private float tileOverlap = 0.02f;   // 타일 사이 틈 방지용 겹침(유닛)

    [Header("Variety")]
    [SerializeField] private float[] tileWeights = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };  // 0~8번 타일 가중치
    [SerializeField] private int randomSeed = 0;                    // 0이면 매번 다른 배치

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -100;

    private Sprite[] tiles;
    private BackgroundTilePool pool;
    private BackgroundTileSelector selector;
    private readonly Dictionary<Vector2Int, SpriteRenderer> activeTiles = new();
    private readonly List<Vector2Int> releaseBuffer = new();

    private Vector2Int currentCell;
    private bool isReady;

    public int CurrentStageNumber { get; private set; }

    private void Start()
    {
        if (target == null && PlayerManager.Instance != null)
            target = PlayerManager.Instance.transform;

        if (target == null)
        {
            Debug.LogError("[InfiniteBackground] 따라갈 대상(Player)을 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        pool = new BackgroundTilePool(transform, sortingLayerName, sortingOrder);
        SetStage(ResolveStageNumber());
    }

    private void Update()
    {
        if (!isReady) return;

        var cell = WorldToCell(target.position);
        if (cell != currentCell)
            Refresh(cell);
    }

    // 스테이지가 바뀌면 타일셋을 교체하고 다시 깐다
    public void SetStage(int stageNumber)
    {
        ReleaseAll();

        CurrentStageNumber = stageNumber;
        tiles = StageTileSetLoader.Load(resourcePathFormat, stageNumber, insetPixels, tileWorldSize);

        if (tiles == null)
        {
            isReady = false;
            return;
        }

        selector = new BackgroundTileSelector(tileWeights, randomSeed);
        isReady = true;

        Refresh(WorldToCell(target.position));
        Debug.Log("[InfiniteBackground] 스테이지 " + stageNumber + " 배경 적용");
    }

    private int ResolveStageNumber()
    {
        if (waveManager != null && waveManager.CurrentStage != null)
            return waveManager.CurrentStage.stageId;

        Debug.LogWarning("[InfiniteBackground] 현재 스테이지를 알 수 없어 기본 스테이지 " + fallbackStageNumber + "을 사용합니다.");
        return fallbackStageNumber;
    }

    private void Refresh(Vector2Int center)
    {
        currentCell = center;

        // 반경 안의 빈 칸에 타일 생성
        for (int y = -activeRadius; y <= activeRadius; y++)
        {
            for (int x = -activeRadius; x <= activeRadius; x++)
            {
                var cell = new Vector2Int(center.x + x, center.y + y);
                if (!activeTiles.ContainsKey(cell))
                    Spawn(cell);
            }
        }

        // 반경 + 여유분보다 멀어진 타일 회수
        int releaseRadius = activeRadius + releaseMargin;
        releaseBuffer.Clear();

        foreach (var cell in activeTiles.Keys)
        {
            if (Mathf.Abs(cell.x - center.x) > releaseRadius || Mathf.Abs(cell.y - center.y) > releaseRadius)
                releaseBuffer.Add(cell);
        }

        foreach (var cell in releaseBuffer)
        {
            pool.Release(activeTiles[cell]);
            activeTiles.Remove(cell);
        }
    }

    private void Spawn(Vector2Int cell)
    {
        var tile = pool.Get();
        tile.sprite = tiles[selector.GetTileIndex(cell)];
        tile.transform.position = CellToWorld(cell);

        float scale = 1f + tileOverlap / tileWorldSize;
        tile.transform.localScale = new Vector3(scale, scale, 1f);

        activeTiles[cell] = tile;
    }

    private void ReleaseAll()
    {
        if (pool == null) return;

        foreach (var tile in activeTiles.Values)
            pool.Release(tile);

        activeTiles.Clear();
    }

    private Vector2Int WorldToCell(Vector3 position)
        => new(Mathf.RoundToInt(position.x / tileWorldSize), Mathf.RoundToInt(position.y / tileWorldSize));

    private Vector3 CellToWorld(Vector2Int cell)
        => new(cell.x * tileWorldSize, cell.y * tileWorldSize, 0f);
}