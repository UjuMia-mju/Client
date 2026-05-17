using UnityEngine;

public class WormTrap : MonoBehaviour
{
    [SerializeField] private GameObject desertWormPrefab;


    // 디버그용: 인스펙터/다른 코드에서 호출하면 스폰 시도
    public void WormTrigger()
    {
        // 호스트만 스폰 권한
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
            return;

        MonsterManager.Instance.SpawnMonster(Monsters.DesertWorm, this.transform.position, this.transform.rotation);
    }
}
