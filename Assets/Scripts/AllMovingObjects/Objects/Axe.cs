using UnityEngine;

public class Axe : Items
{
    private const float HUMAN_HIT_FREEZE_SEC = 0.5f;

    private bool hasUsed = false;

    private void OnTriggerStay(Collider other)
    {
        if (transform.parent == null || transform.parent.name != SOCKET) return;
        if (hasUsed) return;

        Player holderLocal = GetComponentInParent<Player>();
        OtherPlayers holderRemote = holderLocal == null ? GetComponentInParent<OtherPlayers>() : null;
        GameObject holderRoot = holderLocal != null ? holderLocal.gameObject
                              : holderRemote != null ? holderRemote.gameObject
                              : null;
        if (holderRoot == null) return;

        // 1) 나무 벌목 (로컬 holder가 도구 사용 중일 때만)
        if (holderLocal != null && holderLocal.isUsingTool && other.CompareTag(Define.Tag.TREE))
        {
            TreeResource t = other.GetComponent<TreeResource>();
            if (t != null)
            {
                t.Logging();
                hasUsed = true;
            }
            return;
        }

        // 2) 사람 피격: 호스트만 단독 판정.
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;

        bool swinging = holderLocal != null
            ? holderLocal.isUsingTool
            : IsRemoteSwinging(holderRemote);
        if (!swinging) return;

        Player victimLocal = other.GetComponentInParent<Player>();
        OtherPlayers victimRemote = victimLocal == null ? other.GetComponentInParent<OtherPlayers>() : null;
        GameObject victimRoot = victimLocal != null ? victimLocal.gameObject
                              : victimRemote != null ? victimRemote.gameObject
                              : null;
        if (victimRoot == null) return;
        if (victimRoot == holderRoot) return;

        ulong victimId = victimLocal != null
            ? (ulong)NetManager.Instance._playerId
            : victimRemote.PlayerId;

        if (victimLocal != null)
            victimLocal.FreezeFor(HUMAN_HIT_FREEZE_SEC);

        PacketSender.Instance.BroadcastPlayerHit(victimId, HUMAN_HIT_FREEZE_SEC);

        hasUsed = true;
    }

    private static bool IsRemoteSwinging(OtherPlayers remote)
    {
        if (remote == null) return false;
        Animator anim = remote.GetComponentInChildren<Animator>();
        if (anim == null) return false;
        return anim.GetInteger("AnimationPar") == (int)AnimState.Mining;
    }

    public void ResetHasChopped()
    {
        hasUsed = false;
    }
}
