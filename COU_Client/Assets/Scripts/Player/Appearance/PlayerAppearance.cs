using UnityEngine;
using UnityEngine.LowLevel;
using System.Collections;


[RequireComponent(typeof(SPUM_Prefabs))]
public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private WeaponAppearanceSet weaponSet;
    [SerializeField] private SpriteRenderer rightWeapon;
    [SerializeField] private SpriteRenderer leftWeapon;
    [SerializeField] private bool keepInPlace = true;

    [Header("Invulnerable Blink")]
    [SerializeField][Min(0.02f)] private float blinkInterval = 0.1f;   // 깜빡임 간격(초)
    [SerializeField][Range(0f, 1f)] private float blinkAlpha = 0.3f;    // 흐려질 때 투명도 배율
    private SpriteRenderer[] spriteRenderers;
    private float[] baseAlphas;
    private Coroutine blinkRoutine;

    [Header("Test")]
    [SerializeField] private long testItemId = 6001;
    [SerializeField] private int testAttackIndex = 0;

    private SPUM_Prefabs spum;
    private bool isReady;
    private PlayerState currentState = PlayerState.IDLE;
    private float oneShotEndTime;
    private bool isDead;
    private Transform animatedRoot;            // 애니메이션이 움직이는 몸 기준점 (UnitRoot 아래 Root)
    private Vector3 animatorBaseLocalPosition; // UnitRoot의 원래 위치
    private Vector3 rootBaseLocalPosition;     // Root의 원래 위치

    private Sprite defaultRight;
    private Sprite defaultLeft;

    public int AttackIndex { get; private set; }
    public float AttackHitTime { get; private set; }

    private void LateUpdate()
    {
        if (!isReady || !keepInPlace) return;
        if (currentState == PlayerState.DEATH) return;

        ResetPose();
    }

    public void Awake()
    {
        spum = GetComponent<SPUM_Prefabs>();

        defaultRight = rightWeapon != null ? rightWeapon.sprite : null;
        defaultLeft = leftWeapon != null ? leftWeapon.sprite : null;

        if(spum._anim == null)
        {
            Debug.LogError("[PlayerAppearance] SPUM_Prefabs의 Anim(Animator)이 비어 있습니다.", this);
            return;
        }
        // 애니메이션 이동이 캐릭터 위치를 실제로 옮기지 않도록
        spum._anim.applyRootMotion = false;

        // 원래 위치 기억 (피격 등 한 번 재생 모션이 끝나면 되돌림)
        animatorBaseLocalPosition = spum._anim.transform.localPosition;
        animatedRoot = spum._anim.transform.Find("Root");
        if (animatedRoot != null)
            rootBaseLocalPosition = animatedRoot.localPosition;

        spum.OverrideControllerInit();
        isReady = true;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseAlphas = new float[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            baseAlphas[i] = spriteRenderers[i].color.a;
    }

    // === 무기 ===
    public void SetWeapon(long? itemId)
    {
        if (itemId == null)
        {
            SetWeaponSprites(null, null);
            AttackIndex = 0;
            AttackHitTime = 0f;
            return;
        }

        if (weaponSet != null && weaponSet.TryGet(itemId.Value, out var entry))
        {
            SetWeaponSprites(entry.rightHand, entry.leftHand);
            AttackIndex = entry.attackAnimationIndex;
            AttackHitTime = entry.attackHitTime;
            return;
        }

        SetWeaponSprites(defaultRight, defaultLeft);
        AttackIndex = 0;
        AttackHitTime = 0f;
        Debug.LogWarning("[PlayerAppearance] 무기 외형이 등록되지 않았습니다: " + itemId.Value + ". 기본 무기를 표시합니다.", this);
    }

    private void SetWeaponSprites(Sprite right, Sprite left)
    {
        if (rightWeapon != null) rightWeapon.sprite = right;
        if (leftWeapon != null) leftWeapon.sprite = left;
    }

    // ===== 애니메이션 =====

    public void PlayIdle() => PlayLoop(PlayerState.IDLE);
    public void PlayMove() => PlayLoop(PlayerState.MOVE);

    public void PlayAttack()
    {
        if (isDead) return;
        PlayOneShot(PlayerState.ATTACK, AttackIndex);
    }

    public void PlayDamaged()
    {
        if (isDead) return;

        // 피격 모션이 재생 중이면 다시 시작하지 않음 (연속 피해 시 떨림 방지)
        if (currentState == PlayerState.DAMAGED && Time.time < oneShotEndTime) return;

        PlayOneShot(PlayerState.DAMAGED, 0);
    }

    public void PlayDeath()
    {
        if (isDead) return;

        StopBlink();

        Play(PlayerState.DEATH, 0);
        isDead = true;   // 이후 다른 모션 재생 안 함
    }

    // 대기·이동처럼 계속 유지되는 상태: 바뀔 때만, 한 번 재생 모션이 끝난 뒤에만
    private void PlayLoop(PlayerState state)
    {
        if (isDead) return;
        if (Time.time < oneShotEndTime) return;
        if (currentState == state) return;

        ResetPose();

        Play(state, 0);
    }

    private void ResetPose()
    {
        spum._anim.transform.localPosition = animatorBaseLocalPosition;
        if (animatedRoot != null)
            animatedRoot.localPosition = rootBaseLocalPosition;
    }

    // 공격·피격처럼 한 번 재생하는 상태: 클립 길이만큼 대기·이동 전환을 막음
    private void PlayOneShot(PlayerState state, int index)
    {
        var clip = Play(state, index);
        if (clip != null)
            oneShotEndTime = Time.time + clip.length;
    }

    private AnimationClip Play(PlayerState state, int index)
    {
        if (!isReady) return null;

        var clips = GetClips(state);
        if (clips == null || clips.Count == 0)
        {
            Debug.LogWarning("[PlayerAppearance] " + state + " 애니메이션이 없습니다.", this);
            return null;
        }

        if (index < 0 || index >= clips.Count)
        {
            Debug.LogWarning("[PlayerAppearance] " + state + " " + index + "번 애니메이션이 없어 0번을 재생합니다.", this);
            index = 0;
        }

        spum.PlayAnimation(state, index);
        currentState = state;
        return clips[index];
    }

    private System.Collections.Generic.List<AnimationClip> GetClips(PlayerState state) => state switch
    {
        PlayerState.IDLE => spum.IDLE_List,
        PlayerState.MOVE => spum.MOVE_List,
        PlayerState.ATTACK => spum.ATTACK_List,
        PlayerState.DAMAGED => spum.DAMAGED_List,
        PlayerState.DEBUFF => spum.DEBUFF_List,
        PlayerState.DEATH => spum.DEATH_List,
        _ => spum.OTHER_List
    };

    // ===== 깜빡임 (무적 표시) =====

    // duration초 동안 깜빡임. 이미 깜빡이는 중이면 처음부터 다시
    public void PlayBlink(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f) return;

        StopBlink();
        blinkRoutine = StartCoroutine(BlinkRoutine(duration));
    }

    // 깜빡임을 멈추고 원래 투명도로 되돌림
    public void StopBlink()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        SetAlphaMultiplier(1f);
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float endTime = Time.time + duration;
        bool dim = true;

        while (Time.time < endTime)
        {
            SetAlphaMultiplier(dim ? blinkAlpha : 1f);
            dim = !dim;
            yield return new WaitForSeconds(blinkInterval);
        }

        SetAlphaMultiplier(1f);
        blinkRoutine = null;
    }

    // 각 부위의 원래 투명도에 배율을 곱함 (원래 숨겨진 부위는 계속 숨김)
    private void SetAlphaMultiplier(float multiplier)
    {
        if (spriteRenderers == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            var color = spriteRenderers[i].color;
            color.a = baseAlphas[i] * multiplier;
            spriteRenderers[i].color = color;
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 반투명으로 남지 않게
        StopBlink();
    }

    // ===== 테스트 (Inspector ⋮ 메뉴) =====

    [ContextMenu("Test/Idle")] private void TestIdle() => PlayIdle();
    [ContextMenu("Test/Move")] private void TestMove() => PlayMove();
    [ContextMenu("Test/Attack (testAttackIndex)")] private void TestAttack() => PlayOneShot(PlayerState.ATTACK, testAttackIndex);
    [ContextMenu("Test/Damaged")] private void TestDamaged() => PlayDamaged();
    [ContextMenu("Test/Death")] private void TestDeath() => PlayDeath();
    [ContextMenu("Test/Weapon (testItemId)")] private void TestWeapon() => SetWeapon(testItemId);
    [ContextMenu("Test/No Weapon")] private void TestNoWeapon() => SetWeapon(null);

    [ContextMenu("Test/Blink 1초")] private void TestBlink() => PlayBlink(1f);
}
