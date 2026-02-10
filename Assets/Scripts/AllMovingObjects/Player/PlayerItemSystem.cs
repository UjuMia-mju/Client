using UnityEngine;

public class PlayerItemSystem : MonoBehaviour
{
    private GameObject itemSocket;

    private void Start()
    {
        foreach (Transform child in this.transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains("Socket"))
            {
                itemSocket = child.gameObject;
                break;
            }
        }
    }
}