using System;

// 스폰 배치 형태. SpawnPattern.json의 "formation" 문자열을 enum으로 바꿔서 쓴다.
// - Random  : 마리마다 플레이어 주변 원 위의 무작위 위치
// - Ring    : 플레이어 주변 원 위에 같은 간격
// - Line    : 한쪽에 플레이어 방향과 수직인 직선
// - Cluster : 원 위의 한 지점 주변에 뭉침
// - Screen  : 카메라 화면 안의 무작위 위치
public enum SpawnFormation { Random, Ring, Line, Cluster, Screen }

// 스폰된 몬스터의 이동. SpawnPattern.json의 "movement" 문자열을 enum으로 바꿔서 쓴다.
// - Chase  : 플레이어 추적
// - Charge : 스폰 순간의 플레이어 위치로 직진하고, 스폰 거리의 2배를 가면 사라짐(일반 몬스터만)
public enum SpawnMovement { Chase, Charge }

// "어떤 모양으로, 몇 마리를" 뽑을지. Wave.json과 StageRoster.json의 patternId가 참조한다.
[Serializable]
public class SpawnPatternData
{
    public int patternId;
    public string formation;      // "Random" / "Ring" / "Line" / "Cluster" / "Screen"
    public int spawnCount;        // 한 번 스폰할 때의 마릿수
    public float spawnRadius;     // 플레이어로부터의 거리. 0이면 WaveManager 인스펙터 기본값
    public float formationSize;   // Line 전체 길이 / Cluster 퍼짐 반경. 0이면 WaveManager 인스펙터 기본값
    public string movement;       // "Chase" / "Charge"

    [NonSerialized] private SpawnFormation parsedFormation;
    public SpawnFormation Formation => parsedFormation;

    [NonSerialized] private SpawnMovement parsedMovement;
    public SpawnMovement Movement => parsedMovement;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출. 알 수 없는 형태나 이동이면 false
    public bool OnLoaded() => Enum.TryParse(formation, out parsedFormation) && Enum.TryParse(movement, out parsedMovement);
}
