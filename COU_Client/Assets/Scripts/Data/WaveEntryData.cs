using System;

// 웨이브의 한 줄: "이 웨이브의 이 시각부터, 이 패턴으로, 이 슬롯의 몬스터를" 뽑는다.
// 웨이브는 여러 스테이지가 공유하며, 슬롯은 스테이지마다 StageRoster.json이 채운다.
[Serializable]
public class WaveEntryData
{
    public int waveEntryId;
    public int waveId;
    public float patternStartTime;  // 시작 시각(초)
    public float patternDuration;   // 반복 지속 시간(초). 0이면 patternStartTime에 한 번
    public float spawnInterval;     // 반복 간격(초). patternDuration이 0이면 쓰지 않음
    public int patternId;           // SpawnPattern.json 참조
    public string monsterSlot;      // StageRoster.json의 슬롯 이름 (예: "N1", "E1", "B1", "BOX")
}
