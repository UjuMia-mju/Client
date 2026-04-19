using UnityEngine;

public class PlayerItemSystem : MonoBehaviour
{
    public GameObject itemSocket {get; private set;}
    public GameObject currentEquipItem { get; private set; }

    private const float THROW_FORCE = 0.02f;
    private const float MAX_THROW_FORCE = 20f;
    private const float MIN_THROW_FORCE = 5f;
    private const float CHARGED_MAX_THROW_FORCE = 38f;
    private const float CHARGED_MIN_THROW_FORCE = 16f;
    private const float CHARGED_UP_BLEND = 0.3f;
    private const float CONTROL_RUNNINGAMOUNT = 0.15f;
    private const float MIN_RUNNINGAMOUNT = 0.01f;

    private GameObject _lastThrownItem; // 마지막으로 던진 아이템

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

    public Vector3 ComputeThrowImpulse(float runningAmount, Vector3 flatAimDirection, bool chargedThrow = false)
    {
        if (currentEquipItem == null) return Vector3.zero;

        flatAimDirection = Vector3.ProjectOnPlane(flatAimDirection, transform.up);
        if (flatAimDirection.sqrMagnitude < 1e-6f)
            flatAimDirection = Vector3.ProjectOnPlane(transform.forward, transform.up);
        flatAimDirection.Normalize();

        Vector3 forwardVec;
        if (runningAmount < MIN_RUNNINGAMOUNT)
            forwardVec = flatAimDirection;
        else
            forwardVec = flatAimDirection * (runningAmount * CONTROL_RUNNINGAMOUNT);

        float upWeight = chargedThrow ? CHARGED_UP_BLEND : 1f;
        Vector3 force = (transform.up * upWeight + forwardVec) * THROW_FORCE;
        float minF = chargedThrow ? CHARGED_MIN_THROW_FORCE : MIN_THROW_FORCE;
        float maxF = chargedThrow ? CHARGED_MAX_THROW_FORCE : MAX_THROW_FORCE;
        float clampedMagnitude = Mathf.Clamp(force.magnitude, minF, maxF);
        return force.normalized * clampedMagnitude;
    }

    public Vector3 GetThrowStartPosition()
    {
        return itemSocket != null ? itemSocket.transform.position : transform.position;
    }

    public float GetHeldItemMass()
    {
        if (currentEquipItem == null) return 1f;
        Rigidbody rb = currentEquipItem.GetComponent<Rigidbody>();
        return rb != null ? Mathf.Max(0.01f, rb.mass) : 1f;
    }

    public void ThrowItem(float runningAmount)
    {
        ThrowWithImpulse(ComputeThrowImpulse(runningAmount, transform.forward, false));
    }

    public void ThrowItemWithAim(float runningAmount, Vector3 flatAimDirection)
    {
        ThrowWithImpulse(ComputeThrowImpulse(runningAmount, flatAimDirection, false));
    }

    public void ThrowChargedAim(float runningAmount, Vector3 flatAimDirection)
    {
        ThrowWithImpulse(ComputeThrowImpulse(runningAmount, flatAimDirection, true));
    }

    private void ThrowWithImpulse(Vector3 force)
    {
        Rigidbody rb = currentEquipItem.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        ObjectsGravityController objectGravityController = currentEquipItem.GetComponent<ObjectsGravityController>();
        objectGravityController.enabled = true;

        BoxCollider boxCollider = currentEquipItem.GetComponent<BoxCollider>();
        boxCollider.enabled = true;

        this.currentEquipItem.transform.SetParent(null);

        rb.AddForce(force, ForceMode.Impulse);

        _lastThrownItem = currentEquipItem;

        // 던질 때 즉시 위치 동기화 강제
        Items itemClass = currentEquipItem.GetComponent<Items>();
        if (itemClass != null)
            itemClass.OnDetached();

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

    public Items GetCurrentEquipItemClass()
    {
        if (this.currentEquipItem != null)
            return currentEquipItem.GetComponent <Items>();

        return null;
    }

    public GameObject GetLastThrownItem()
    {
        return _lastThrownItem;
    }
}