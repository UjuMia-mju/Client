using UnityEngine;

// 아이템의 곡괭이로써의 기능을 담당하는 클래스입니다.
public class Pickaxe : Items
{
    private const float HUMAN_HIT_FREEZE_SEC = 0.5f;

    private bool hasMined = false; // 이미 채굴/타격했는지 여부

    private void OnTriggerStay(Collider other)
    {
        if (transform.parent == null || transform.parent.name != SOCKET) return;
        if (hasMined) return;

        Player holderLocal = GetComponentInParent<Player>();
        OtherPlayers holderRemote = holderLocal == null ? GetComponentInParent<OtherPlayers>() : null;
        GameObject holderRoot = holderLocal != null ? holderLocal.gameObject
                              : holderRemote != null ? holderRemote.gameObject
                              : null;
        if (holderRoot == null) return;

        // 1) 광석 채굴 (로컬 holder가 도구 사용 중일 때만)
        if (holderLocal != null && holderLocal.isUsingTool && other.CompareTag(Define.Tag.ORE))
        {
            Ore o = other.GetComponent<Ore>();
            if (o != null) { o.Mine(); hasMined = true; }
            return;
        }

        // 2) 사람 피격: 호스트만 단독 판정. 피어 측 trigger는 무시.
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;

        // 휘두르는 중일 때만 사람 hit 인정 (들고만 있거나 던지는 중에는 무시)
        bool swinging = holderLocal != null
            ? holderLocal.isUsingTool
            : (holderRemote != null && holderRemote.GetAnimStateRaw() == (int)AnimState.Mining);
        if (!swinging) return;

        // victim 식별 (콜라이더가 자식에 있을 수 있어 InParent 사용)
        Player victimLocal = other.GetComponentInParent<Player>();
        OtherPlayers victimRemote = victimLocal == null ? other.GetComponentInParent<OtherPlayers>() : null;
        GameObject victimRoot = victimLocal != null ? victimLocal.gameObject
                              : victimRemote != null ? victimRemote.gameObject
                              : null;
        if (victimRoot == null) return; // 사람 아님

        // 자해 차단
        if (victimRoot == holderRoot) return;

        ulong victimId = victimLocal != null
            ? (ulong)NetManager.Instance._playerId
            : victimRemote.PlayerId;

        // 호스트 본인이 victim이면 broadcast echo가 자기에게 안 오므로 직접 freeze.
        if (victimLocal != null)
            victimLocal.FreezeFor(HUMAN_HIT_FREEZE_SEC);

        PacketSender.Instance.BroadcastPlayerHit(victimId, HUMAN_HIT_FREEZE_SEC);

        hasMined = true;
    }

    public void ResetHasMined()
    {
        hasMined = false;
    }

    private void Update()
    {
        // holder가 더 이상 swing 중이 아니면 hasMined 자동 해제.
        // 로컬 Player의 EndMining은 호스트 머신의 OtherPlayers 손 곡괭이까지 못 닿으므로 여기서 보강.
        if (!hasMined) return;

        Player holderLocal = GetComponentInParent<Player>();
        if (holderLocal != null)
        {
            if (!holderLocal.isUsingTool) hasMined = false;
            return;
        }

        OtherPlayers holderRemote = GetComponentInParent<OtherPlayers>();
        if (holderRemote != null)
        {
            if (holderRemote.GetAnimStateRaw() != (int)AnimState.Mining)
                hasMined = false;
            return;
        }

        // 누구의 손에도 안 들려있으면 reset.
        hasMined = false;
    }
}
