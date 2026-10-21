using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// =====================================================================
//  [로컬 전용] 계정 데이터 - 서버 전환 시 삭제
// =====================================================================

/// <summary>
/// 로컬에 저장할 계정 목록 리스트
/// </summary>
[Serializable]
public class AccountSaveData
{
    public List<LocalAccountData> accounts = new List<LocalAccountData>();
}

/// <summary>
/// [로컬 전용] 서버 연동 전 임시 로그인 정보. 서버 전환 시 삭제.
/// playerId = 로그인 아이디 (DB 문서의 accountLoginId에 해당, DB의 playerId(int)와 다름)
/// password = 평문 (로컬 테스트 전용, 서버에서는 BCrypt 해시로 저장)
/// </summary>
[Serializable]
public class LocalAccountData
{
    public string playerId;
    public string password;
}

// =====================================================================
//  [서버] 계정 데이터 (데이터 정의서 > Account)
// =====================================================================

/// <summary>
/// [서버] 로그인 성공 시 서버가 내려주는 계정 정보.
/// Account 테이블 기준이지만, 비밀번호 해시(accountPasswordHash)는
/// 서버 DB에만 존재하며 클라이언트로 내려오지 않는다.
/// </summary>
[Serializable]
public class ServerAccountData
{
    public int accountId;                  // PK, Auto Increment (서버 부여)
    public string accountLoginId;          // 로그인 아이디, Unique
    public DateTime accountCreatedAt;      // 계정 생성 시각
    public DateTime? accountLastLoginAt;   // 마지막 로그인 시각 (Null 허용)
    // accountPasswordHash: 서버 전용 필드 (클라이언트 보관 금지)
}

// =====================================================================
//  플레이어 게임 데이터 (PlayerProfile / Currency / Inventory / StageProgress / PlayerStat)
//  로컬: Players/{로그인아이디}.json 으로 저장
//  서버: "세이브 불러오기" API 응답 형식
// =====================================================================

[Serializable]
public class PlayerSaveData
{
    public PlayerProfileData profile = new PlayerProfileData();
    public CurrencyData currency = new CurrencyData();
    public List<InventoryData> inventoryList = new List<InventoryData>();
    public StageProgressData stageProgress = new StageProgressData();
    public List<StageRecordSaveData> stageRecords = new List<StageRecordSaveData>();   // 기록이 있는 스테이지만
    public List<int> claimedChapterChests = new List<int>();   // 챕터 보물상자를 받은 스테이지 ID
    public PlayerStatData playerStat = new PlayerStatData();   // 필드만

    /// <summary>
    /// [로컬 전용] 회원가입 시 초기 데이터 생성.
    /// 서버 모드에서는 서버가 초기 데이터를 만들어 내려준다.
    /// accountId, playerId는 로컬에서 순번(accounts.Count + 1)으로 부여해 Auto Increment를 흉내 낸다.
    /// </summary>
    public static PlayerSaveData CreateDefault(int accountId, long playerId, string playerNickname)
    {
        if (accountId <= 0 || playerId <= 0)
            throw new ArgumentException("accountId와 playerId가 필요합니다.");
        if (string.IsNullOrWhiteSpace(playerNickname))
            throw new ArgumentException("닉네임이 필요합니다.");

        return new PlayerSaveData
        {
            profile = new PlayerProfileData
            {
                playerId = playerId,
                accountId = accountId,
                playerNickname = playerNickname,
                accountLevel = 1,
                accountExp = 0
                // equipped{Slot}InventoryId: 생성 시 Null (순환 참조라 이후 업데이트)
            },
            currency = new CurrencyData
            {
                playerId = playerId,
                currencyGold = 0,
                currencyGem = 0,
                currencyEnergy = 0,            // 필드만 (문서 기본값 0)
                currencyEnergyUpdatedAt = null
            },
            inventoryList = new List<InventoryData>(),
            stageProgress = new StageProgressData
            {
                playerId = playerId,
                currentStageId = GetFirstStageId(),
                maxClearedStageId = null       // 클리어 이력 없으면 Null
            },
            playerStat = new PlayerStatData { playerId = playerId }
        };
    }

    // 스테이지 ID는 해금 순서대로 증가한다는 전제 (데이터 공통 규칙 9번)
    private static int GetFirstStageId()
    {
        var stageDataDic = GameManager.JsonData.StageDataDic;

        if (stageDataDic == null || stageDataDic.Count == 0)
        {
            Debug.LogWarning("[PlayerSaveData] 스테이지 데이터가 없어 첫 스테이지를 지정하지 못했습니다.");
            return 0;
        }

        return stageDataDic.Keys.Min();
    }
}

/// <summary>PlayerProfile: 플레이어 기본 정보와 장착 장비</summary>
[Serializable]
public class PlayerProfileData
{
    public long playerId;                   // PK, Auto Increment
    public int accountId;                  // FK → Account, Unique
    public string playerNickname;          // Unique
    // playerIcon: 문서만 → 필드 생성 안 함

    public int accountLevel = 1;           // 접두어 예외 (클라이언트와 이름 통일)
    public int accountExp;                 // 현재 레벨 기준 경험치 (레벨업 시 차감)
}

/// <summary>Currency: 보유 재화. 1:1이므로 playerId가 PK</summary>
[Serializable]
public class CurrencyData
{
    public long playerId;                      // PK, FK → PlayerProfile
    public int currencyGold;                  // 음수 불가
    public int currencyGem;                   // 음수 불가
    public int currencyEnergy;                // 필드만 (충전 규칙 미정)
    public DateTimeOffset? currencyEnergyUpdatedAt; // UTC, 서버가 회복 기준으로 쓴다 (Null 허용)
}

/// <summary>Inventory: 보유 장비 (같은 장비 중복 보유 가능, 인스턴스 방식)</summary>
[Serializable]
public class InventoryData
{
    public long inventoryId;                // PK, Auto Increment
    public long playerId;                   // FK → PlayerProfile
    public long itemId;                     // 논리 FK → Item (long → int)
    public int inventoryItemLevel = 1;     // 최대값은 ItemConst.maxLevel
    public DateTime inventoryAcquiredAt;   // 획득 시각 (정렬용)

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemGrade? inventoryItemGrade;

    [JsonProperty("isEquipped")] 
    public bool inventoryEquipped; // 장착 여부 ( 서버 Equipment.equipped )
}

/// <summary>StageProgress: 스테이지 진행 정보. 1:1이므로 playerId가 PK</summary>
[Serializable]
public class StageProgressData
{
    public long playerId;                   // PK, FK → PlayerProfile
    public int currentStageId;             // 논리 FK → Stage
    public int? maxClearedStageId;         // 논리 FK → Stage, 클리어 이력 없으면 Null

    // 순서대로 해금되므로 클리어 여부는 ID 비교로 판단 (isCleared 목록 대체)
    public bool IsCleared(int stageId) => maxClearedStageId.HasValue && stageId <= maxClearedStageId.Value;
}

/// <summary>PlayerStat: 스탯 강화 진행도 (필드만, 재설계 예정)</summary>
[Serializable]
public class PlayerStatData
{
    public long playerId;                   // PK, FK → PlayerProfile
    public int playerStatAttackLevel;
    public int playerStatHpLevel;
    public int playerStatDefenseLevel;
    public int playerStatPotionRecoveryLevel;
}

/// <summary>StageRecord: 스테이지별 최장 생존 시간. 승패와 관계없이 서버가 갱신한다</summary>
[Serializable]
public class StageRecordSaveData
{
    public int stageId;
    public int bestSurvivalSeconds;
}