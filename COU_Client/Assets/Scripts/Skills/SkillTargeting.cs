using System.Collections.Generic;
using UnityEngine;

// 스킬이 적을 찾을 때 쓰는 공용 탐색
// 결과 리스트를 하나만 두고 재사용해서 탐색할 때마다 배열을 새로 만들지 않음 (적이 많을 때 GC 부담 방지)
// 반환한 리스트는 다음 탐색에서 덮어쓰므로, 받은 즉시 순회하고 순회 도중에 다시 탐색하지 말 것
public static class SkillTargeting
{
    private static readonly List<Collider2D> _results = new(128);

    private static ContactFilter2D _enemyFilter;
    private static bool _initialized;

    // center에서 radius 안에 있는 적 콜라이더 목록
    public static List<Collider2D> OverlapEnemies(Vector2 center, float radius)
    {
        EnsureInitialized();
        Physics2D.OverlapCircle(center, radius, _enemyFilter, _results);

        return _results;
    }

    // center에서 radius 안의 살아 있는 적 중 가장 가까운 적. exclude에 든 적은 제외하고, 없으면 null
    public static Enemy FindNearestEnemy(Vector2 center, float radius, HashSet<Enemy> exclude = null)
    {
        Enemy nearest = null;
        float minSqrDistance = float.MaxValue;

        foreach(Collider2D other in OverlapEnemies(center, radius))
        {
            if(!other.TryGetComponent(out Enemy enemy) || enemy.isDead || (exclude != null && exclude.Contains(enemy)))
            {
                continue;
            }

            float sqrDistance = ((Vector2)enemy.transform.position - center).sqrMagnitude;

            if(sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    // LayerMask.GetMask는 정적 필드 초기화 시점에 부를 수 없으므로 첫 탐색 때 준비
    private static void EnsureInitialized()
    {
        if(_initialized)
        {
            return;
        }

        _initialized = true;

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
        _enemyFilter.useTriggers = true;
    }
}
