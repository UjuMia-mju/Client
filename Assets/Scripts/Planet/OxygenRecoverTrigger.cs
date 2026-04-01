using UnityEngine;

public class OxygenRecoverTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PeerPlayerStat tempStat = other.GetComponent<PeerPlayerStat>();
            tempStat.StartOxygenRecovery();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            PeerPlayerStat tempStat = other.GetComponent<PeerPlayerStat>();
            tempStat.StopOxygenRecovery();
        }
    }
}