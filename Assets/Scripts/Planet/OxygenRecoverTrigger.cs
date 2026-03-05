using UnityEngine;

public class OxygenRecoverTrigger : MonoBehaviour
{
    private Coroutine oxygenCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PlayerStat tempStat = other.GetComponent<PlayerStat>();
            oxygenCoroutine = StartCoroutine(tempStat.OxygenIncrease());
            Debug.Log("트리거 : 산소");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            if (oxygenCoroutine != null)
            {
                StopCoroutine(oxygenCoroutine);
                oxygenCoroutine = null;
            }
            Debug.Log("트리거 : 산소 나감");
        }
    }
}