using UnityEngine;

// 야구 배트: 휘두를 때 몬스터(DesertWorm 등)의 체력을 깎는 무기
public class BaseballBat : Items
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;

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

        if (!other.CompareTag(Define.Tag.MONSTER)) return;

        Player holderLocal = GetComponentInParent<Player>();
        OtherPlayers holderRemote = holderLocal == null ? GetComponentInParent<OtherPlayers>() : null;
        if (holderLocal == null && holderRemote == null) return;

        bool swinging = holderLocal != null
            ? (holderLocal.isUsingTool && IsInHitWindow(holderLocal.GetComponent<Animator>()))
            : IsInHitWindow(holderRemote.GetComponent<Animator>());
        if (!swinging) return;

        Monster victim = other.GetComponentInParent<Monster>();
        if (victim == null) return;

        victim.TakeDamage(damage);
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
