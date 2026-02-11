using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Progress;

public class Objects : MovingObject
{
    private Player target;
    private void LateUpdate()
    {
        if (target != null && target.isGetItem)
        {
            this.transform.position = target.playerItemSystem.itemSocket.transform.position;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(Define.Tag.PLAYER))
        {
            Player player = other.GetComponent<Player>();

            if (!player.isGetItem)
            {
                target = player;
                player.GetItem(gameObject);
            }
        }
    }
}