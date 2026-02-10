using System.Collections;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    private int oxygen = 100;

    public IEnumerator OxygenDecrease()
    {
        while (true)
        {
            oxygen -= 1;
            Debug.Log("산소 줄어듬 : " + oxygen);
            yield return new WaitForSeconds(1f);
        }
    }
}