using UnityEngine;

/// <summary>
/// 구형 행성 표면에서 우주선(로켓) 방향을 HUD로 표시합니다.
/// 행성 중심(바깥이 '위')을 기준으로, 표면에서의 <b>최단(대원) 경로 초기 접선 방향</b>과
/// 플레이어의 시선(접선으로 투영) 사이의 각도를 <see cref="indicatorRect"/>의 x에 매핑합니다.
/// </summary>
public class SpaceShipLocationController : MonoBehaviour
{
    [Header("행성")]
    [Tooltip("PlanetGravity가 붙은 행성 오브젝트(중심)")]
    [SerializeField] private Transform planetCenter;

    [Header("World")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform spaceshipTransform;

    [Header("UI")]
    [Tooltip("Canvas의 Image")]
    [SerializeField] private RectTransform indicatorRect;
    
    private GameObject hudRoot;

    [Tooltip("방위를 x에 매핑할 때 ±180°를 이 구간으로 둡니다.")]
    [SerializeField] private float minX = -880f;
    [SerializeField] private float maxX = 880f;
    
    [SerializeField] private GameObject readyToStartPanel;

    void Awake()
    {
        if (hudRoot == null)
            hudRoot = gameObject;

        if (indicatorRect != null && indicatorRect.gameObject == gameObject)
        {
            Debug.LogWarning(
                "[SpaceShipLocationController] indicator와 같은 오브젝트에 붙이면 Ready 구간에서 SetActive(false)로 업데이트가 멈춥니다. " +
                "Canvas 등 항상 켜 둔 부모에 스크립트를 두고, 움직이는 UI만 indicator로 지정하세요.");
        }
    }

    void OnEnable()
    {
        GameplayReadyCoordinator.WhenGateReleased(OnGameplayReadyGateReleased);
    }

    void OnDisable()
    {
        GameplayReadyCoordinator.CancelWhenGateReleased(OnGameplayReadyGateReleased);
    }

    void Start()
    {
        if (planetCenter == null)
        {
            PlanetGravity pg = FindAnyObjectByType<PlanetGravity>();
            if (pg != null)
                planetCenter = pg.transform;
        }
    }

    /// <summary>ReadyToStart 게이트가 풀리면 호출 — SpaceShipLocation HUD를 켭니다.</summary>
    void OnGameplayReadyGateReleased()
    {
        if (hudRoot != null && !hudRoot.activeSelf)
            hudRoot.SetActive(true);

        if (indicatorRect != null && !indicatorRect.gameObject.activeSelf
            && !ReadyUiShouldHide())
            indicatorRect.gameObject.SetActive(true);
    }

    bool ReadyUiShouldHide()
    {
        return GameplayReadyCoordinator.IsGateBlocking
               || (readyToStartPanel != null && readyToStartPanel.activeInHierarchy);
    }

    void LateUpdate()
    {
        if (indicatorRect == null)
            return;

        bool hide = ReadyUiShouldHide();

        if (hide)
        {
            if (indicatorRect.gameObject.activeSelf)
                indicatorRect.gameObject.SetActive(false);
            return;
        }

        if (!indicatorRect.gameObject.activeSelf)
            indicatorRect.gameObject.SetActive(true);

        if (playerTransform == null || spaceshipTransform == null)
            return;

        Vector3 surfaceUp = ResolveSurfaceUp();

        // 표면 접선: 플레이어가 바라보는 방향(구 위쪽 기준 평면에 투영)
        Vector3 flatFwd = Vector3.ProjectOnPlane(playerTransform.forward, surfaceUp);
        if (flatFwd.sqrMagnitude < 1e-10f)
            flatFwd = Vector3.ProjectOnPlane(playerTransform.right, surfaceUp);
        if (flatFwd.sqrMagnitude < 1e-10f)
            return;
        flatFwd.Normalize();

        Vector3 flatToShip;
        if (planetCenter != null)
        {
            flatToShip = GreatCircleTangentOnSphere(
                surfaceUp,
                spaceshipTransform.position,
                planetCenter.position);
        }
        else
        {
            flatToShip = Vector3.ProjectOnPlane(
                spaceshipTransform.position - playerTransform.position,
                surfaceUp);
        }

        if (flatToShip.sqrMagnitude < 1e-10f)
        {
            SetIndicatorX(0f);
            return;
        }
        flatToShip.Normalize();

        float signedDeg = Vector3.SignedAngle(flatFwd, flatToShip, surfaceUp);
        float t = Mathf.Clamp(signedDeg / 180f, -1f, 1f);
        float x = Mathf.LerpUnclamped(minX, maxX, (t + 1f) * 0.5f);
        x = Mathf.Clamp(x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));

        SetIndicatorX(x);
    }

    /// <summary>플레이어 발 아래 바깥 방향(행성 반경). PlanetGravity와 동일: (플레이어 - 중심).normalized</summary>
    Vector3 ResolveSurfaceUp()
    {
        if (planetCenter != null)
            return (playerTransform.position - planetCenter.position).normalized;

        return playerTransform.up.normalized;
    }

    /// <summary>
    /// 플레이어 위치의 접선 평면에서, 중심을 지나 A→B 대원으로 갈 때의 초기 방향.
    /// </summary>
    static Vector3 GreatCircleTangentOnSphere(Vector3 radialOutPlayer, Vector3 shipWorldPos, Vector3 planetWorldCenter)
    {
        Vector3 a = radialOutPlayer.normalized;
        Vector3 b = (shipWorldPos - planetWorldCenter).normalized;
        // b를 접선 평면에 정류(평행 성분 제거) → 대원 접선
        Vector3 tangent = b - Vector3.Dot(b, a) * a;
        if (tangent.sqrMagnitude < 1e-12f)
            tangent = Vector3.ProjectOnPlane(shipWorldPos - (planetWorldCenter + a * Vector3.Dot(shipWorldPos - planetWorldCenter, a)), a);
        return tangent;
    }

    void SetIndicatorX(float x)
    {
        Vector2 p = indicatorRect.anchoredPosition;
        p.x = x;
        indicatorRect.anchoredPosition = p;
    }

    /// <summary>런임에 플레이어/우주선 참조를 갈아끼울 때 사용.</summary>
    public void SetWorldBindings(Transform player, Transform spaceship)
    {
        playerTransform = player;
        spaceshipTransform = spaceship;
    }

    public void SetPlanetCenter(Transform center)
    {
        planetCenter = center;
    }
}
