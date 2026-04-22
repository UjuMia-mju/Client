using UnityEngine;

public class PlayerItemSystem : MonoBehaviour
{
    public GameObject itemSocket {get; private set;}
    public GameObject currentEquipItem { get; private set; }

    [Header("Throw - Base")]
    [SerializeField] private float throwForce = 0.02f;
    [SerializeField] private float maxThrowForce = 20f;
    [SerializeField] private float minThrowForce = 5f;
    [SerializeField] private float controlRunningAmount = 0.15f;
    [SerializeField] private float minRunningAmount = 0.01f;

    [Header("Throw - Charged (RMB + LMB)")]
    [SerializeField] private float chargedMaxThrowForce = 38f;
    [SerializeField] private float chargedMinThrowForce = 16f;
    [SerializeField] private float chargedUpBlend = 0.3f;

    [Header("Throw - Vertical Angle")]
    [SerializeField] private float throwPitchSensitivity = 0.9f;
    [SerializeField] private float minUpWeight = 0.1f;
    [SerializeField] private float maxUpWeight = 1.9f;

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

        Items itemClass = item.GetComponent<Items>();
        if (itemClass != null)
            itemClass.SetOwnedByMe(true);

        this.currentEquipItem = item;
    }

    public Vector3 ComputeThrowImpulse(float runningAmount, Vector3 aimDirection, bool chargedThrow = false)
    {
        if (currentEquipItem == null) return Vector3.zero;

        Vector3 up = transform.up;
        Vector3 flatAimDirection = Vector3.ProjectOnPlane(aimDirection, up);
        if (flatAimDirection.sqrMagnitude < 1e-6f)
            flatAimDirection = Vector3.ProjectOnPlane(transform.forward, up);
        flatAimDirection.Normalize();

        Vector3 forwardVec;
        if (runningAmount < minRunningAmount)
            forwardVec = flatAimDirection;
        else
            forwardVec = flatAimDirection * (runningAmount * controlRunningAmount);

        // 카메라 pitch(위/아래 시선)를 던지기 각도에 반영합니다.
        float verticalDot = Mathf.Clamp(Vector3.Dot(aimDirection.normalized, up), -1f, 1f);
        float upWeightBase = chargedThrow ? chargedUpBlend : 1f;
        float upWeight = Mathf.Clamp(upWeightBase + verticalDot * throwPitchSensitivity, minUpWeight, maxUpWeight);

        Vector3 force = (up * upWeight + forwardVec) * throwForce;
        float minF = chargedThrow ? chargedMinThrowForce : minThrowForce;
        float maxF = chargedThrow ? chargedMaxThrowForce : maxThrowForce;
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

    // 원격 플레이어에서 DROP 수신 시: 로컬에서 임의 물리를 주지 않고 네트워크 위치 동기화만 따릅니다.
    public void DetachForRemoteSync()
    {
        if (currentEquipItem == null) return;

        Rigidbody rb = currentEquipItem.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        ObjectsGravityController objectGravityController = currentEquipItem.GetComponent<ObjectsGravityController>();
        if (objectGravityController != null)
            objectGravityController.enabled = false;

        BoxCollider boxCollider = currentEquipItem.GetComponent<BoxCollider>();
        if (boxCollider != null)
            boxCollider.enabled = true;

        currentEquipItem.transform.SetParent(null);

        Items itemClass = currentEquipItem.GetComponent<Items>();
        if (itemClass != null)
            itemClass.OnDetached(false);

        DetachItem();
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
        {
            itemClass.OnDetached(true);
            PacketSender.Instance.SendItemMove(itemClass.itemId, currentEquipItem.transform.position, currentEquipItem.transform.rotation);
        }

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