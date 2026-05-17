using UnityEngine;

public class DesertWorm : MovingObject
{
    public int monsterId;
    private int hp = 3;
    private int attack = 1;

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
