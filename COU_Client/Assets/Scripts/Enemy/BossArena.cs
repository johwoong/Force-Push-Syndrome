using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BossArena : MonoBehaviour
{
    public static BossArena Current { get; private set; }

    [Header("References")]
    [SerializeField] private MonsterSpawner spawner;   // 씬의 MonsterSpawner
    [SerializeField] private Rigidbody2D playerBody;   // Player의 Rigidbody2D

    [Header("Arena")]
    [SerializeField] private float radius = 6f;               // 결계 반지름
    [SerializeField] private float playerBodyRadius = 0.3f;   // 플레이어 몸 크기
    [SerializeField] private float bossBodyRadius = 0.6f;     // 보스 몸 크기
    [SerializeField, Range(0f, 1f)] private float bossSpawnRatio = 0.7f;  // 보스를 중심에서 반지름의 몇 % 위치에 둘지

    [Header("Visual")]
    [SerializeField] private int segments = 64;          
    [SerializeField] private float lineWidth = 0.15f;

    private LineRenderer line;
    private Vector2 center;
    private Enemy boss;
    private Coroutine clampRoutine;

    public bool IsActive { get; private set; }
    public Vector2 Center => center;
    public float Radius => radius;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;    // 결계 오브젝트 위치와 상관없이 월드 좌표로 그림
        line.loop = true;             // 마지막 점과 첫 점을 이어서 닫힌 원으로
        line.positionCount = segments;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.enabled = false;         // 평소엔 숨김
    }

    // 켜질 때 구독
    private void OnEnable()
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<MonsterSpawner>();
        }
            
        if (spawner == null)
        {
            Debug.LogWarning("[BossArena] 씬에서 MonsterSpawner를 찾지 못했습니다.");
            return;
        }

        spawner.OnBossSpawned += HandleBossSpawned;
        spawner.OnBossKilled += HandleBossKilled;
    }

    // 꺼질 때 해제
    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.OnBossSpawned -= HandleBossSpawned;
            spawner.OnBossKilled -= HandleBossKilled;
        }
        Deactivate();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // 보스 등장 시 플레이어 위치를 중심으로 결계 생성
    private void HandleBossSpawned(Enemy spawnedBoss)
    {
        if (playerBody == null)
        {
            playerBody = FindPlayerBody();
        }

        if (playerBody == null)
        {
            Debug.LogWarning("[BossArena] Player의 Rigidbody2D를 찾지 못했습니다.");
            return;
        }

        boss = spawnedBoss;
        Activate(playerBody.position);

        Vector2 dir = ((Vector2)boss.transform.position - center).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        boss.transform.position = center + dir * (radius * bossSpawnRatio);
    }

    // 플레이어를 찾음
    private Rigidbody2D FindPlayerBody()
    {
        if (PlayerManager.Instance == null) { return null; }

        return PlayerManager.Instance.GetComponent<Rigidbody2D>();
    }

    // 보스 처치 → 결계 해제
    private void HandleBossKilled(Enemy _)
    {
        Deactivate();
    }

    // 결계 활성화
    private void Activate(Vector2 arenaCenter)
    {
        center = arenaCenter;
        IsActive = true;
        Current = this;

        DrawCircle();
        line.enabled = true;

        if (clampRoutine != null) StopCoroutine(clampRoutine);
        clampRoutine = StartCoroutine(ClampPlayerAfterPhysics());

        Debug.Log($"[BossArena] 결계 생성 center={center}, radius={radius}");
    }

    // 결계 비활성화
    private void Deactivate()
    {
        if (!IsActive) { return; }

        IsActive = false;
        if (Current == this) Current = null;

        if (line != null) line.enabled = false;
        boss = null;

        if (clampRoutine != null)
        {
            StopCoroutine(clampRoutine);
            clampRoutine = null;
        }

        Debug.Log("[BossArena] 결계 해제");
    }

    // 보스는 Update에서 transform으로 움직이므로, 그 뒤인 LateUpdate에서 위치를 바로잡는다
    private void LateUpdate()
    {
        if (!IsActive) { return; }

        if (spawner != null && !spawner.IsBossBattle)
        {
            Deactivate();
            return;
        }

        if (boss != null && !boss.isDead)
            boss.transform.position = ClampInside(boss.transform.position, bossBodyRadius);
    }

    private IEnumerator ClampPlayerAfterPhysics()
    {
        var wait = new WaitForFixedUpdate();   
        while (IsActive)
        {
            yield return wait;
            ClampPlayer();
        }
    }

    private void ClampPlayer()
    {
        if (playerBody == null) { return; }

        float limit = radius - playerBodyRadius;
        Vector2 offset = playerBody.position - center;

        if (offset.sqrMagnitude <= limit * limit) { return; }

        Vector2 normal = offset.normalized;     
        playerBody.position = center + normal * limit;

        // 바깥으로 향하는 속도만 없앰
        Vector2 velocity = playerBody.linearVelocity;
        float outward = Vector2.Dot(velocity, normal); 
        if (outward > 0f) playerBody.linearVelocity = velocity - normal * outward;
    }

    // 위치를 결계 안쪽으로 제한한 결과를 돌려줌(몸 크기만큼 안쪽까지)
    public Vector2 ClampInside(Vector2 position, float bodyRadius)
    {
        float limit = Mathf.Max(0f, radius - bodyRadius);
        Vector2 offset = position - center;
        if (offset.sqrMagnitude <= limit * limit) { return position; }
        return center + offset.normalized * limit;
    }

    // 보스가 이 위치로 이동해도 결계 안인지(돌진 중 벽 충돌 판정용)
    public bool CanBossMoveTo(Vector2 position)
    {
        float limit = Mathf.Max(0f, radius - bossBodyRadius);
        return (position - center).sqrMagnitude <= limit * limit;
    }

    // 원 둘레의 점 segments개를 계산해서 LineRenderer에 넣음
    private void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;  
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 c = IsActive ? (Vector3)center : transform.position;
        Gizmos.DrawWireSphere(c, radius);
    }
}