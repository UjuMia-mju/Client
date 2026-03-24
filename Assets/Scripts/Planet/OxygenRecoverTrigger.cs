using UnityEngine;

public class OxygenRecoverTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PlayerStat stat = other.GetComponent<PlayerStat>();
            if (stat != null)
            {
                stat.StartOxygenRecover();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PlayerStat stat = other.GetComponent<PlayerStat>();
            if (stat != null)
            {
                stat.StopOxygenRecover();
            }
        }
    }
}