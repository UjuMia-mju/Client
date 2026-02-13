using System.Collections;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    private int oxygen = 100;
    private int hp = 5;

    public IEnumerator OxygenDecrease()
    {
        while (true)
        {
            oxygen -= 1;
            Debug.Log("산소 줄어듬 : " + oxygen);
            yield return new WaitForSeconds(1f);
        }
    }

    public void DecreaseHp(int damage)
    {
        hp -= damage;
        Debug.Log("체력 줄어듬 : " + hp);
    }
}