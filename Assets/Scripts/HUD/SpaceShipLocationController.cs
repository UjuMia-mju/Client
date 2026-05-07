using UnityEngine;

/// <summary>
/// 구형 행성 표면에서 우주선(로켓) 위치를 HUD로 표시합니다.
/// 행성 중심(바깥이 '위')을 기준으로, 표면에서의 <b>최단(대원) 경로 초기 접선 방향</b>을
/// 플레이어 시선과 무관한 고정 축(동/북)으로 투영해 <see cref="indicatorRect"/>의 x/y에 매핑합니다.
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

    [Tooltip("동/서 성분을 이 구간으로 매핑합니다.")]
    [SerializeField] private float minX = -880f;
    [SerializeField] private float maxX = 880f;
    [Tooltip("남/북 성분을 이 구간으로 매핑합니다.")]
    [SerializeField] private float minY = -950f;
    [SerializeField] private float maxY = -140f;
    
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
            SetIndicatorPosition(0f, 0f);
            return;
        }
        flatToShip.Normalize();

        Vector3 north = Vector3.ProjectOnPlane(Vector3.forward, surfaceUp);
        if (north.sqrMagnitude < 1e-10f)
            north = Vector3.ProjectOnPlane(Vector3.right, surfaceUp);
        if (north.sqrMagnitude < 1e-10f)
            north = Vector3.ProjectOnPlane(Vector3.up, surfaceUp);
        if (north.sqrMagnitude < 1e-10f)
            return;
        north.Normalize();

        Vector3 east = Vector3.Cross(surfaceUp, north);
        if (east.sqrMagnitude < 1e-10f)
            return;
        east.Normalize();

        float x01 = Mathf.Clamp01(Vector3.Dot(flatToShip, east) * 0.5f + 0.5f);
        float y01 = Mathf.Clamp01(Vector3.Dot(flatToShip, north) * 0.5f + 0.5f);

        float x = Mathf.LerpUnclamped(minX, maxX, x01);
        float y = Mathf.LerpUnclamped(minY, maxY, y01);
        x = Mathf.Clamp(x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        y = Mathf.Clamp(y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

        SetIndicatorPosition(x, y);
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

    void SetIndicatorPosition(float x, float y)
    {
        Vector2 p = indicatorRect.anchoredPosition;
        p.x = x;
        p.y = y;
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
