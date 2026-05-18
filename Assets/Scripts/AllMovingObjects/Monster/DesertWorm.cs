using UnityEngine;

public class DesertWorm : Monster
{

    //public void OnHit()
    //{
        
    //}

    private void Update()
    {
        Dead();
    }

    private void Dead()
    { 
        // 호스트만 제거 권한
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost)
            return;

        if (hp <= 0)
        {
            MonsterManager.Instance.MonsterDead(monsterId);
        }
    }
}
