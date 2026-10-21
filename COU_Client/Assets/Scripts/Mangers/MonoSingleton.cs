using System;
using UnityEngine;

public static class GameManager
{
    public static GameSceneManager Scene { get { return GameSceneManager.Instance; } }
    public static ObjectPoolManager ObjectPool { get { return ObjectPoolManager.Instance; } }
    public static SoundManager Sound { get { return SoundManager.Instance; } }
    public static PlayerDataManager PlayerData { get { return PlayerDataManager.Instance; } }
    public static JsonDataManager JsonData { get { return JsonDataManager.Instance; } }
    public static DropItemManager DropItem { get { return DropItemManager.Instance; } }
    public static UIManager UI { get { return UIManager.Instance; } }
    public static ServerLoginManager ServerLogin { get { return ServerLoginManager.Instance; } }
    public static ServerLoadManager ServerLoad { get { return ServerLoadManager.Instance; } }
    public static ServerInventoryManager ServerInventory { get { return ServerInventoryManager.Instance; } }
    public static ServerShopManager ServerShop { get { return ServerShopManager.Instance; } }
    public static ServerRankingManager ServerRanking { get { return ServerRankingManager.Instance; } }
    public static ServerAchievementManager ServerAchievement { get { return ServerAchievementManager.Instance; } }
    public static ServerFriendManager ServerFriend { get { return ServerFriendManager.Instance; } }
    public static ServerPresenceManager ServerPresence { get { return ServerPresenceManager.Instance; } }
    public static ServerChatManager ServerChat { get { return ServerChatManager.Instance; } }

    [Obsolete("서버 연동으로 사용하지 않음. 접근하면 매니저가 자동 생성되니 쓰지 말 것")]
    public static LocalSaveLoadManager LocalSaveLoad { get { return LocalSaveLoadManager.Instance; } }
    [Obsolete("서버 연동으로 사용하지 않음. 접근하면 매니저가 자동 생성되니 쓰지 말 것")]
    public static LocalLoginManager LocalLogin { get { return LocalLoginManager.Instance; } }
}

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool isDontDestroy = false;
    
    private static T instance;

    // 인스턴스를 새로 만들지 않고 이미 있는지만 확인. 씬 정리(OnDisable/OnDestroy) 중 구독 해제할 때는 Instance 대신 이걸 써야 함
    // (Instance는 없으면 GameObject를 새로 만들어서 "Some objects were not cleaned up when closing the scene" 경고가 남)
    public static bool HasInstance => instance != null;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>() ?? new GameObject(typeof(T).Name).AddComponent<T>();
            }

            return instance;
        }
    }
    
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            
            if (isDontDestroy) DontDestroyOnLoad(transform.root.gameObject);
        }

        else if (instance != this)
        {
            Transform root = transform.root;
            
            bool cleanUpRoot = isDontDestroy
                && root != transform
                && root != instance.transform.root;

            if (cleanUpRoot) transform.SetParent(null);

            Destroy(gameObject);

            if (cleanUpRoot && root.childCount == 0)
                Destroy(root.gameObject);
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
