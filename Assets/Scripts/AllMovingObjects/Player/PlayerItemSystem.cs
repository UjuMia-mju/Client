using UnityEngine;

public class PlayerItemSystem : MonoBehaviour
{
    public GameObject itemSocket {get; private set;}
    public GameObject currentEquipItem { get; private set; }

    private const float THROW_FORCE = 0.02f;
    private const float MAX_THROW_FORCE = 20f;
    private const float MIN_THROW_FORCE = 5f;
    private const float CONTROL_RUNNINGAMOUNT = 0.15f;
    private const float MIN_RUNNINGAMOUNT = 0.01f;

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

    private void LateUpdate()
    {
        if (currentEquipItem != null)
        {
            currentEquipItem.transform.position = itemSocket.transform.position;
        }
    }

    public void AttachItem(GameObject item)
    {
        // 플레이어의 손에 잡힐 때 문제가 되는 요소들을 모두 비활성화
        Rigidbody rb = item.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        // 해당 오브젝트의 중력 제어 비활성화
        ObjectsGravityController objectGravityController = item.GetComponent<ObjectsGravityController>();
        objectGravityController.enabled = false;

        BoxCollider boxCollider = item.GetComponent<BoxCollider>();
        boxCollider.enabled = false;

        item.transform.SetParent(itemSocket.transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        this.currentEquipItem = item;
    }

    public void ThrowItem(float runningAmount)
    {
        // 비활성화된 요소들을 활성화
        Rigidbody rb = currentEquipItem.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        ObjectsGravityController objectGravityController = currentEquipItem.GetComponent<ObjectsGravityController>();
        objectGravityController.enabled = true;

        BoxCollider boxCollider = currentEquipItem.GetComponent<BoxCollider>();
        boxCollider.enabled = true;

        this.currentEquipItem.transform.SetParent(null);

        Vector3 forwardVec;

        if (runningAmount < MIN_RUNNINGAMOUNT)
        {
            forwardVec = transform.forward;
        }
        else
        {
            forwardVec = transform.forward * runningAmount * CONTROL_RUNNINGAMOUNT;
        }


        Vector3 force = (this.transform.up + forwardVec) * THROW_FORCE;

        float clampedMagnitude = Mathf.Clamp(force.magnitude, MIN_THROW_FORCE, MAX_THROW_FORCE);

        force = force.normalized * clampedMagnitude;

        rb.AddForce(force, ForceMode.Impulse);

        // 참조 끊기
        DetachItem();
    }

    public void DetachItem()
    {
        this.currentEquipItem = null;
    }

    public string GetItemTag()
    {
        if (this.currentEquipItem != null)
        {
            return currentEquipItem.tag;
        }
        else
        {
            return null;
        }
    }
}