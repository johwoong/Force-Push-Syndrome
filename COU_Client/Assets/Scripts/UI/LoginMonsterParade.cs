using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 로그인 화면 가운데로 몬스터들이 일렬로 지나가는 연출
// Monster.json의 외형 프리팹을 줄 세워 왼쪽으로 걷게 하고, 화면 왼쪽 밖으로 나가면 줄 맨 뒤(오른쪽 밖)로 보낸다.
public class LoginMonsterParade : MonoBehaviour
{
    [SerializeField] private float spacing = 1.4f;      // 몬스터 사이 간격(유닛)
    [SerializeField] private float moveSpeed = 1.2f;    // 이동 속도(유닛/초)
    [SerializeField] private float monsterScale = 0.6f;

    private static readonly int WalkHash = Animator.StringToHash("Walk");

    private readonly List<Transform> monsters = new();
    private float leftBound;    // 이보다 왼쪽으로 가면 줄 맨 뒤로 보냄
    private float lineLength;   // 줄 전체 길이

    private void Start()
    {
        List<GameObject> prefabs = GameManager.JsonData.MonsterDataDic.Values
            .OrderBy(data => data.monsterId)
            .Select(data => data.monsterAsset)
            .Distinct()
            .Select(path => Resources.Load<GameObject>(path))
            .Where(prefab => prefab != null)    // 스프라이트 외형(상자)은 프리팹이 아니라 제외
            .ToList();

        if (prefabs.Count == 0) return;

        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        leftBound = -halfWidth - spacing;

        // 줄이 화면보다 짧으면 끊겨 보이므로 화면 폭 + 양쪽 여유만큼은 채운다
        int count = Mathf.Max(prefabs.Count, Mathf.CeilToInt(halfWidth * 2f / spacing) + 2);
        lineLength = count * spacing;

        for (int i = 0; i < count; i++)
        {
            GameObject monster = Instantiate(prefabs[i % prefabs.Count], transform);
            monster.transform.localPosition = new Vector3(-halfWidth + i * spacing, 0f, 0f);
            monster.transform.localScale = Vector3.one * monsterScale;   // 에셋 원본이 왼쪽을 봄

            if (monster.TryGetComponent(out Animator animator)) animator.Play(WalkHash);

            monsters.Add(monster.transform);
        }
    }

    private void Update()
    {
        float move = moveSpeed * Time.deltaTime;

        foreach (Transform monster in monsters)
        {
            Vector3 position = monster.localPosition;
            position.x -= move;
            if (position.x < leftBound) position.x += lineLength;
            monster.localPosition = position;
        }
    }
}
