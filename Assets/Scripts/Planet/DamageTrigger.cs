using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageTrigger : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Define.Tag.PLAYER))
            return;

        // 로컬 플레이어: 호스트/피어 모두 stat.DecreaseHp (피어는 서버로 전달)
        PlayerStat stat = other.GetComponentInParent<PlayerStat>();
        if (stat != null && other.GetComponentInParent<Player>() != null)
        {
            if (damageAmount > 0)
                stat.DecreaseHp(damageAmount);
            return;
        }

        // 원격 플레이어: 호스트에서만 권위 데미지
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
            return;

        OtherPlayers remotePlayers = other.GetComponentInParent<OtherPlayers>();
        if (remotePlayers != null && damageAmount > 0)
            HostStatManager.Instance?.DecreaseHp(remotePlayers.PlayerId, damageAmount);
    }
}
