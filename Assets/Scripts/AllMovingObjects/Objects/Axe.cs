using UnityEngine;

public class Axe : Items
{
    private bool hasUsed = false;

    private void OnTriggerStay(Collider other)
    {
        if (transform.parent != null && transform.parent.name == SOCKET)
        {
            Player player = GetComponentInParent<Player>();
            if (player != null)
            {
                if (!hasUsed && player.isUsingTool && other.CompareTag(Define.Tag.TREE))
                {
                    Tree t = other.GetComponent<Tree>();
                    if (t != null)
                    {
                        t.Logging();
                        hasUsed = true;
                    }
                }
            }
        }
    }

    public void ResetHasChopped()
    {
        hasUsed = false;
    }
}
