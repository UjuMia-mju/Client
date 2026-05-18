using UnityEngine;

// 삽: TreasureResource를 파서 보석(또는 트랩으로 DesertWorm)을 드랍시키는 도구
public class Shovel : Items
{
    private bool hasDug = false;

    private void OnTriggerStay(Collider other)
    {
        if (transform.parent == null || transform.parent.name != SOCKET) return;
        if (hasDug) return;

        Player holderLocal = GetComponentInParent<Player>();
        if (holderLocal == null) return;
        if (!holderLocal.isUsingTool) return;

        if (!other.CompareTag(Define.Tag.TREASURE_TROVE)) return;

        TreasureResource treasure = other.GetComponent<TreasureResource>();
        if (treasure == null) treasure = other.GetComponentInParent<TreasureResource>();
        if (treasure == null) return;

        treasure.Dig();
        hasDug = true;
    }

    private void Update()
    {
        if (!hasDug) return;

        Player holderLocal = GetComponentInParent<Player>();
        if (holderLocal != null)
        {
            if (!holderLocal.isUsingTool) hasDug = false;
            return;
        }

        OtherPlayers holderRemote = GetComponentInParent<OtherPlayers>();
        if (holderRemote != null)
        {
            // Shovel은 Digging 상태일 때만 swing 인정
            if (holderRemote.GetAnimStateRaw() != (int)AnimState.Digging)
                hasDug = false;
            return;
        }

        hasDug = false;
    }

    public void ResetHasDug()
    {
        hasDug = false;
    }
}
