/// <summary>
/// 게임에서 사용되는 모든 상수값들 모아놓은 스크립트
/// </summary>

public static class GameConstants
{
    // 씬 이름 상수
    public static class SceneNames
    {
        public const string MANAGERS_TEST_SCENE = "ManagersTestScene";
        public const string MANAGERS_TEST_SCENE_2 = "ManagersTestScene_2";
        public const string LOGIN_SCENE = "LogInScene";
        public const string MAIN_SCENE = "MainScene";
        public const string BATTLE_SCENE = "BattleScene";
    }
    
    // 파일 경로 관련 상수
    public static class Paths
    {
        public const string ACCOUNT_SAVE_PATH = "accounts.json";
        public const string PLAYER_DIRECTORY = "Players";
        public const string MonsterAttackData_Json_Path = "Data/MonsterAttack";
        public const string WaveData_Json_Path = "Data/Wave";
        public const string SpawnPatternData_Json_Path = "Data/SpawnPattern";
        public const string DropTableData_Json_Path = "Data/DropTable";
        public const string StageData_Json_Path = "Data/Stage";
        public const string StageRosterData_Json_Path = "Data/StageRoster";
        public const string MonsterData_Json_Path = "Data/Monster";
        public const string ItemData_Json_Path = "Data/Item";
        public const string EquipEffectData_Json_Path = "Data/EquipEffect";
        public const string ItemGradeEffectData_Json_Path = "Data/ItemGradeEffect";
        public const string SkillData_Json_Path = "Data/Skill";
        public const string SkillLevelData_Json_Path = "Data/SkillLevel";
        public const string SkillIcon_Path = "Skill/Icons/";
        public const string BGM_PATH = "Sound/BGM/";   // Resources/Sound/BGM/{name}
        public const string SFX_PATH = "Sound/SFX/";   // Resources/Sound/SFX/{name}
        public const string DropItemData_Json_Path = "Data/DropItem";
        public const string AccountConstData_Json_Path = "Data/AccountConst";
        public const string PlayerBaseStatData_Json_Path = "Data/PlayerBaseStat";
        public const string ItemConstData_Json_Path = "Data/ItemConst";
        public const string EvolutionData_Json_Path = "Data/Evolution";
        public const string CombatPowerData_Json_Path = "Data/CombatPower";
        public const string STATIC_DATA_DIRECTORY = "StaticData";
        public const string STATIC_DATA_VERSION_FILE = "version.txt";
        public const string AchievementData_Json_Path = "Data/Achievement";
        
        public const string LOGIN_PASSWORD_ICON_SHEET = "UI/Icon_Login_Password";
        public const string LOGIN_PASSWORD_ICON_MASKED = "Icon_Login_Password_Visible";      // * 표시 중 → 눈동자
        public const string LOGIN_PASSWORD_ICON_UNMASKED = "Icon_Login_Password_Unvisible";  // * 표시 해제 → 빗금 눈동자
    }

    // 서버 통신 상수
    public static class Server
    {
#if UNITY_EDITOR
        public const string BASE_URL = "http://localhost:8080";   // 에디터: 로컬 서버
#else
        public const string BASE_URL = "http://3.34.14.51:8080";  // 빌드: AWS EC2 (cou-server, 탄력적 IP)
#endif
        public const string STATIC_DATA_API = "/api/static-data";
        // 이 빌드가 읽을 수 있는 정적 데이터 버전의 첫째 자리
        public const int STATIC_DATA_MAJOR_VERSION = 5;
        public const string SIGNUP_API = "/api/accounts/signup";
        public const string LOGIN_API = "/api/accounts/login";
        public const string PLAYER_SAVE_API = "/api/players/me/save";
        public const string INVENTORY_API = "/api/inventory";   // Server 클래스에 추가
    }

    // 배경음 이름 (Resources/Sound/BGM/{이름})
    public static class Bgm
    {
        public const string MAIN = "Main";            // 로그인·메인 씬
        private const string STAGE_PREFIX = "Stage";  // 전투: Stage1, Stage2 ... (스테이지 id 그대로)

        public static string ForStage(int stageId) => STAGE_PREFIX + stageId;

        // 씬 전체가 한 곡을 쓰는 씬의 곡 이름. 전투 씬은 스테이지에 따라 달라서 null
        public static string ForScene(string sceneName)
            => sceneName == SceneNames.LOGIN_SCENE || sceneName == SceneNames.MAIN_SCENE ? MAIN : null;
    }

    // 효과음 이름 (Resources/Sound/SFX/{이름})
    public static class Sfx
    {
        // UI (Resources/Sound/SFX/UI/폴더)
        public const string CLICK = "UI/Click";                   // 모든 버튼 공통 (ButtonClickSound가 자동 재생)
        public const string EQUIP = "UI/Equip";                   // 장비 장착
        public const string UNEQUIP = "UI/Unequip";               // 장비 해제
        public const string EQUIP_LEVEL_UP = "UI/EquipLevelUp";   // 장비 레벨업
        public const string SYNTHESIZE = "UI/Synthesize";         // 장비 합성
        public const string EVOLVE = "UI/Evolve";                 // 진화
        public const string ACHIEVEMENT = "UI/Achievement";       // 업적 보상 수령
        public const string BUY_SUPPLY_BOX = "UI/BuySupplyBox";   // 전리품 상자 구매
        public const string BUY_GOLD = "UI/BuyGold";              // 골드 구매
        public const string BUY_GEM = "UI/BuyGem";                // 젬 구매

        // 전투 효과음 (Resources/Sound/SFX/Battle/ 폴더)
        public const string PLAYER_HIT = "Battle/PlayerHit";         // 플레이어 피격
        public const string GOLD = "Battle/Gold";                    // 골드 획득
        public const string POTION = "Battle/Potion";                // 포션 획득(회복)
        public const string MAGNET = "Battle/Magnet";                // 자석 획득
        public const string BOMB = "Battle/Bomb";                    // 폭탄 획득(폭발)
        public const string LEVEL_UP = "Battle/LevelUp";             // 스킬 선택창 열림
        public const string LUCK_TRAIN_TICK = "Battle/LuckTrainTick";     // 행운열차 칸 이동
        public const string LUCK_TRAIN_RESULT = "Battle/LuckTrainResult"; // 행운열차 결과
        public const string STAGE_CLEAR = "Battle/StageClear";       // 스테이지 클리어
        public const string STAGE_FAIL = "Battle/StageFail";         // 스테이지 실패
    }

    // PlayerPrefs 저장 키
    public static class PrefsKeys
    {
        // 자동 로그인 세션 (AuthSessionStore)
        public const string AUTH_ACCESS_TOKEN = "COU.Auth.AccessToken";
        public const string AUTH_ACCOUNT = "COU.Auth.Account";
    }

    public static class Value
    {
        public const int REQUEST_TIMEOUT_SECONDS = 10;
        public const long CONNECTION_FAILED = 0;
        public const long ALREADY_REQUESTING = -1; 
        public const float MIN_LOADING_SECONDS = 1f;   // 추가: 로딩이 빨라도 최소 이 시간은 로딩창을 보여준다
    }

    public static class TemporaryConstClass
    {
        // 액티브
        public const long TUMBLER = 20010;
        public const long GLASSES = 20020;
        public const long MONITOR = 20030;
        public const long SPEAKER = 20040;
        public const long MOUSE = 20050;
        public const long KEYBOARD = 20060;
        public const long CPU = 20070;
        public const long POWER_STRIP = 20080;
        public const long CTRL_Z = 20090;

        // 패시브
        public const long ENERGY_DRINK = 21010;
        public const long EYE_DROPS = 21020;
        public const long SECOND_MONITOR = 21030;
        public const long EARBUDS = 21040;
        public const long MOUSE_PAD = 21050;
        public const long KEYBOARD_SWITCH = 21060;
        public const long OVERCLOCK = 21070;
        public const long STATIC_SHOCK = 21080;
        public const long SHIFT_KEY = 21090;
    }
}
