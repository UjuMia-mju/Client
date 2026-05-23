using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Define.Tag.PLAYER)) return;
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;

        // GetComponent → GetComponentInParent 로 변경 (자식 콜라이더 대응)
        Player localPlayer = other.GetComponentInParent<Player>();
        if (localPlayer != null)
        {
            PlayerStat stat = localPlayer.GetComponent<PlayerStat>();
            if (stat != null) stat.DecreaseHp(1);
            return;
        }

        // 원격 플레이어 (피어)
        OtherPlayers remotePlayers = other.GetComponentInParent<OtherPlayers>();
        if (remotePlayers != null)
        {
            HostStatManager.Instance.DecreaseHp(remotePlayers.PlayerId, 1);
        }
    }
}