using UnityEngine;

// 야구 배트: 휘두를 때 몬스터(DesertWorm 등)의 체력을 깎는 무기.
// 사람을 휘두르면 Pickaxe 와 동일하게 일정 시간 기절(Surprise/Freeze)시킨다.
public class BaseballBat : Items
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;

    private const float HUMAN_HIT_FREEZE_SEC = 0.5f;

    // Swing 애니메이션 사이클 중 hit를 인정할 구간 (0~1 정규화 시간).
    private const float HIT_WINDOW_START = 0.3f;
    private const float HIT_WINDOW_END = 0.6f;

    private bool hasHit = false;

    private bool IsInHitWindow(Animator anim)
    {
        if (anim == null) return false;
        var info = anim.GetCurrentAnimatorStateInfo(0);
        if (anim.GetInteger("AnimationPar") != (int)AnimState.Mining) return false;

        float t = info.normalizedTime % 1f;
        return t >= HIT_WINDOW_START && t <= HIT_WINDOW_END;
    }

    private void OnTriggerStay(Collider other)
    {
        if (transform.parent == null || transform.parent.name != SOCKET) return;
        if (hasHit) return;

        // 호스트 단독 판정 (피어 측 trigger는 무시)
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;

        // holder 식별
        Player holderLocal = GetComponentInParent<Player>();
        OtherPlayers holderRemote = holderLocal == null ? GetComponentInParent<OtherPlayers>() : null;
        GameObject holderRoot = holderLocal != null ? holderLocal.gameObject
                              : holderRemote != null ? holderRemote.gameObject
                              : null;
        if (holderRoot == null) return;

        // 휘두르는 중에만 hit 인정
        bool swinging = holderLocal != null
            ? (holderLocal.isUsingTool && IsInHitWindow(holderLocal.GetComponent<Animator>()))
            : IsInHitWindow(holderRemote.GetComponent<Animator>());
        if (!swinging) return;

        // 1) 몬스터 타격
        if (other.CompareTag(Define.Tag.MONSTER))
        {
            Monster victim = other.GetComponentInParent<Monster>();
            if (victim == null) return;

            victim.TakeDamage(damage);
            hasHit = true;
            return;
        }

        // 2) 사람 타격 → 기절(Freeze) 동기화. Pickaxe 와 동일 정책.
        Player victimLocal = other.GetComponentInParent<Player>();
        OtherPlayers victimRemote = victimLocal == null ? other.GetComponentInParent<OtherPlayers>() : null;
        GameObject victimRoot = victimLocal != null ? victimLocal.gameObject
                              : victimRemote != null ? victimRemote.gameObject
                              : null;
        if (victimRoot == null) return;

        // 자해 차단
        if (victimRoot == holderRoot) return;

        ulong victimId = victimLocal != null
            ? (ulong)NetManager.Instance._playerId
            : victimRemote.PlayerId;

        // 호스트 본인이 victim이면 broadcast echo가 자기에게 안 오므로 직접 freeze.
        if (victimLocal != null)
            victimLocal.FreezeFor(HUMAN_HIT_FREEZE_SEC);

        PacketSender.Instance.BroadcastPlayerHit(victimId, HUMAN_HIT_FREEZE_SEC);

        hasHit = true;
    }

    private void Update()
    {
        if (!hasHit) return;

        Player holderLocal = GetComponentInParent<Player>();
        if (holderLocal != null)
        {
            if (!holderLocal.isUsingTool) hasHit = false;
            return;
        }

        OtherPlayers holderRemote = GetComponentInParent<OtherPlayers>();
        if (holderRemote != null)
        {
            if (holderRemote.GetAnimStateRaw() != (int)AnimState.Mining)
                hasHit = false;
            return;
        }

        hasHit = false;
    }

    public void ResetHasHit()
    {
        hasHit = false;
    }
}
