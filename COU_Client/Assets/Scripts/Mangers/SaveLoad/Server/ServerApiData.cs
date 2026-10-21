using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// 서버 API 요청/응답 JSON 형식 (필드 이름 = JSON 키)
// 실패 응답 ErrorResponse는 Network/ErrorResponse.cs에 있음

// ===== 계정 =====

[Serializable]
public class SignupRequest
{
    public string accountLoginId;
    public string password;
}

[Serializable]
public class LoginRequest
{
    public string accountLoginId;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string accessToken;
    public string tokenType;               // "Bearer"
    public ServerAccountData account;      // SaveData.cs의 기존 클래스 재사용
}

// ===== 전투 (dev) =====

[Serializable]
public class BattleEnterRequest
{
    public int stageId;
}

[Serializable]
public class BattleEnterResponse
{
    public long battleId;
    public CurrencyData currency;          // 차감 후 재화
}

// StageId는 서버가 battleId로 알고, 계정 경험치는 서버가 계산하므로 보내지 않는다
[Serializable]
public class BattleResultRequest
{
    public bool victory;
    public int seconds;
    public int kills;
    public int gold;
    public int rewardBoxes;
    
    // 업적 진행 집계
    public Dictionary<long, int> skillKills = new();
    public Dictionary<long, int> skillUpgrades = new();
    public Dictionary<string, int> pickups = new();
    public int luckTrains;   // 행운열차 완주 횟수
}

[Serializable]
public class BattleResultResponse
{
    public int grantedGold;                // 상한 적용 후 지급한 골드
    public int grantedExp;                 // 지급한 계정 경험치
    public PlayerProfileData profile;
    public CurrencyData currency;
    public StageProgressData stageProgress;
    public StageRecordSaveData stageRecord;
    public List<InventoryData> rewards;    // 보상상자로 지급한 장비
}

// POST /api/battles/patrol 응답. 최고 클리어 스테이지를 한 번 클리어한 평균 보상
[Serializable]
public class PatrolResponse
{
    public int stageId;                    // 순찰한 스테이지 (최고 클리어 스테이지)
    public int grantedGold;
    public int grantedExp;
    public PlayerProfileData profile;
    public CurrencyData currency;          // 스태미나 차감, 골드 지급 후 재화
    public List<InventoryData> rewards;    // 보상상자로 지급한 장비
}

// ===== 챕터 보물상자 =====

// POST /api/chapter-chests/{stageId}/claim 응답
[Serializable]
public class ChapterChestClaimResponse
{
    public int stageId;
    public List<InventoryData> rewardedItems;
}

// ===== 인벤토리 / 장비 =====

// GET /api/inventory 응답
[Serializable]
public class InventoryResponse
{
    public int currencyGold;                 // /me/save의 currency와 같은 값이라 사용하지 않음
    public int currencyGem;
    public List<InventoryData> items;
}

// POST /api/inventory/{id}/equip
[Serializable]
public class EquipResponse
{
    public InventoryData equipped;      // 장착된 장비
    public InventoryData unequipped;    // 같은 슬롯에서 자동 해제된 장비 (없으면 null)
}

// POST /api/inventory/{id}/levelup
[Serializable]
public class LevelUpResponse
{
    public long inventoryId;
    public int inventoryItemLevel;   // 레벨업 후 레벨
    public int spentGold;
    public int currencyGold;         // 차감 후 보유 골드
}

// POST /api/inventory/{id}/levelup/batch
[Serializable]
public class LevelUpBatchResponse
{
    public long inventoryId;
    public int inventoryItemLevel;   // 최종 레벨
    public int levelsGained;
    public int spentGold;
    public int currencyGold;
}

// POST /api/inventory/{id}/synthesize, /synthesize/batch (batch는 tiersGained가 추가됨)
[Serializable]
public class SynthesizeResponse
{
    public long inventoryId;
    public long itemId;
    public int inventoryItemLevel;

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemGrade? inventoryItemGrade;   // 합성 후 등급

    public bool isEquipped;
    public int tiersGained;                 // 일괄 합성에서만 값이 옴
    public List<long> consumedInventoryIds; // 재료로 소모된 장비
}

// ===== 상점 =====
// POST /api/shop/purchase 요청
[Serializable]
public class PurchaseRequest
{
    public int productId;
}

// POST /api/shop/purchase 응답
[Serializable]
public class PurchaseResponse
{
    public int productId;
    public int spentGem;
    public int currencyGold;                    // 구매 후 보유 골드 
    public int currencyGem;                     // 구매 후 보유 보석 
    public List<InventoryData> rewardedItems;   // 장비 상자일 때만 값이 있음
}

// ===== 랭킹 =====

public enum RankingType { ACCOUNT_LEVEL, MAX_STAGE, STAGE_SURVIVAL, COMBAT_POWER }

[Serializable]
public class RankingEntryResponse
{
    public int rank;
    public long playerId;
    public string nickname;
    public int accountLevel;
    public int value;          // 종류별 점수 (레벨 / 최고 스테이지 / 생존 초)
}

// GET /api/ranking 응답
[Serializable]
public class RankingResponse
{
    public string type;
    public int? stageId;
    public List<RankingEntryResponse> entries;
    public int? myRank;        // 기록 없으면 null
    public int? myValue;
}

// ===== 친구 =====

public enum FriendRelation { NONE, FRIEND, REQUEST_SENT, REQUEST_RECEIVED }

// 친구 한 명 (친구 목록, 수락 응답)
[Serializable]
public class FriendResponse
{
    public long playerId;
    public string nickname;
    public int accountLevel;
    public long combatPower;
    public bool online;
}

// GET /api/friends 응답
[Serializable]
public class FriendListResponse
{
    public List<FriendResponse> friends;
    public int maxFriends;
}

// GET /api/friends/requests 응답 한 줄 (받은 요청)
[Serializable]
public class FriendRequestResponse
{
    public long requestId;
    public long playerId;
    public string nickname;
    public int accountLevel;
}

// GET /api/friends/search 응답 한 줄
[Serializable]
public class FriendSearchResponse
{
    public long playerId;
    public string nickname;
    public int accountLevel;
    public FriendRelation relation;
}

// POST /api/friends/requests 요청 / 응답
[Serializable]
public class FriendRequestBody
{
    public long targetPlayerId;
}

[Serializable]
public class FriendRequestResultResponse
{
    public FriendRelation relation; // REQUEST_SENT 또는 FRIEND(상대가 먼저 보낸 상태라 바로 친구) /
}


// GET/POST /api/chat/{friendPlayerId}/messages 응답
[Serializable]
public class ChatMessageResponse
{
    public long messageId;
    public long senderId;
    public bool mine;           // 내가 보낸 메시지인지
    public string content;
    public DateTime createdAt; 
}

[Serializable]
public class ChatSendRequest
{
    public string content;
}