using UnityEngine;


public class Monster : MovingObject
{
    [HideInInspector] public int monsterId;
    protected int hp = 3;
    protected int attack = 1;

    void Start() { }
    void Update() { }

    // 호스트 전용: 외부(무기)에서 몬스터에 데미지 적용
    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        hp -= amount;
        if (hp < 0) hp = 0;
    }
}
