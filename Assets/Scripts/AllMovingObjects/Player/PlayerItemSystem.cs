using System.Collections;
using UnityEngine;

public class PlayerItemSystem : MonoBehaviour
{
    public GameObject itemSocket {get; private set;}
    public GameObject currentEquipItem { get; private set; }
    private const float THROWER_IGNORE_COLLISION_DURATION = 0.65f;

    [Header("Throw - Base")]
    [Tooltip("기본 던지기 벡터에 곱해지는 스케일. 전체 세기 체감에 가장 큰 영향을 줍니다.")]
    [SerializeField] private float throwForce = 0.02f;
    [Tooltip("일반(F) 던지기의 최대 힘.")]
    [SerializeField] private float maxThrowForce = 20f;
    [Tooltip("일반(F) 던지기의 최소 힘. 정지 상태에서도 이 값 이상으로 던져집니다.")]
    [SerializeField] private float minThrowForce = 5f;
    [Tooltip("이동 속도를 전방 힘으로 변환할 때 곱하는 계수.")]
    [SerializeField] private float controlRunningAmount = 0.15f;
    [Tooltip("이동 보정이 시작되는 최소 속도 임계값.")]
    [SerializeField] private float minRunningAmount = 0.01f;

    [Header("Throw - Charged (RMB + LMB)")]
    [Tooltip("강한 던지기(우클릭+좌클릭)의 최대 힘.")]
    [SerializeField] private float chargedMaxThrowForce = 38f;
    [Tooltip("강한 던지기(우클릭+좌클릭)의 최소 힘.")]
    [SerializeField] private float chargedMinThrowForce = 16f;
    [Tooltip("강한 던지기 기본 상승 비율. 낮을수록 더 낮고 멀리 날아갑니다.")]
    [Range(0f, 2f)]
    [SerializeField] private float chargedUpBlend = 0.3f;

    [Header("Throw - Vertical Angle")]
    [Tooltip("마우스 위/아래(pitch)가 상승 비율에 반영되는 민감도.")]
    [SerializeField] private float throwPitchSensitivity = 0.9f;
    [Tooltip("상승 비율의 최소값.")]
    [SerializeField] private float minUpWeight = 0.1f;
    [Tooltip("상승 비율의 최대값.")]
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
        // [수정] 피어 측에서는 로컬 물리 throw를 수행하지 않는다.
        //   - 던지기 권한은 호스트(피어 자신을 대역하는 OtherPlayers)에게 있고
        //     호스트가 권위 물리 시뮬레이션 후 S_OBJECT_MOVE로 위치를 동기화한다.
        //   - 피어가 로컬에서 isKinematic=false + AddForce를 적용하면 호스트와
        //     별개의 물리 궤적이 만들어지고, 착지 시 velocity가 0에 근접하는
        //     순간 Items.Moving()의 MovePosition(Lerp(pos, stale, 0.5)) 분기가
        //     발사되어 지면 침투 → 물리 솔버가 위로 튕김(팝콘 현상)을 유발한다.
        bool isPeer = ConnectManager.Instance != null && !ConnectManager.Instance.isHost;

        if (isPeer)
        {
            // 시각적 detach만 수행. 물리는 호스트가 결정 → S_OBJECT_MOVE 추종.
            Rigidbody rbPeer = currentEquipItem.GetComponent<Rigidbody>();
            if (rbPeer != null)
                rbPeer.isKinematic = true;

            ObjectsGravityController peerGrav = currentEquipItem.GetComponent<ObjectsGravityController>();
            if (peerGrav != null)
                peerGrav.enabled = false;

            BoxCollider peerBox = currentEquipItem.GetComponent<BoxCollider>();
            if (peerBox != null)
                peerBox.enabled = true;

            currentEquipItem.transform.SetParent(null);
            StartCoroutine(TemporarilyIgnoreThrowerCollision(currentEquipItem));

            _lastThrownItem = currentEquipItem;

            Items itemClassPeer = currentEquipItem.GetComponent<Items>();
            if (itemClassPeer != null)
                itemClassPeer.OnDetached(false); // 피어는 송신 권한 없음 → ownedByMeAfterDetach=false

            DetachItem();
            return;
        }

        // ↓ 호스트(권위 측) 경로: 기존 로직 유지 ↓
        Rigidbody rb = currentEquipItem.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        ObjectsGravityController objectGravityController = currentEquipItem.GetComponent<ObjectsGravityController>();
        objectGravityController.enabled = true;

        BoxCollider boxCollider = currentEquipItem.GetComponent<BoxCollider>();
        boxCollider.enabled = true;

        this.currentEquipItem.transform.SetParent(null);
        StartCoroutine(TemporarilyIgnoreThrowerCollision(currentEquipItem));

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

    private IEnumerator TemporarilyIgnoreThrowerCollision(GameObject thrownItem)
    {
        if (thrownItem == null) yield break;

        SetIgnoreCollisionWithThrower(thrownItem, true);
        yield return new WaitForSeconds(THROWER_IGNORE_COLLISION_DURATION);
        SetIgnoreCollisionWithThrower(thrownItem, false);
    }

    private void SetIgnoreCollisionWithThrower(GameObject thrownItem, bool ignore)
    {
        if (thrownItem == null) return;

        Collider[] throwerColliders = GetComponentsInChildren<Collider>(true);
        Collider[] itemColliders = thrownItem.GetComponentsInChildren<Collider>(true);
        if (throwerColliders == null || itemColliders == null) return;

        for (int i = 0; i < throwerColliders.Length; i++)
        {
            Collider throwerCollider = throwerColliders[i];
            if (throwerCollider == null) continue;

            for (int j = 0; j < itemColliders.Length; j++)
            {
                Collider itemCollider = itemColliders[j];
                if (itemCollider == null) continue;

                Physics.IgnoreCollision(throwerCollider, itemCollider, ignore);
            }
        }
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

    /// <summary>
    /// 동일한 던지기 체감을 유지하기 위해 다른 PlayerItemSystem의 튜닝값을 복사합니다.
    /// (예: 호스트에서 OtherPlayers 대역 throw 시 로컬 Player와 같은 수치 사용)
    /// </summary>
    public void CopyThrowTuningFrom(PlayerItemSystem source)
    {
        if (source == null) return;

        throwForce = source.throwForce;
        maxThrowForce = source.maxThrowForce;
        minThrowForce = source.minThrowForce;
        controlRunningAmount = source.controlRunningAmount;
        minRunningAmount = source.minRunningAmount;
        chargedMaxThrowForce = source.chargedMaxThrowForce;
        chargedMinThrowForce = source.chargedMinThrowForce;
        chargedUpBlend = source.chargedUpBlend;
        throwPitchSensitivity = source.throwPitchSensitivity;
        minUpWeight = source.minUpWeight;
        maxUpWeight = source.maxUpWeight;
    }
}