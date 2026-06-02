using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1001)]
public class CustomCursorController : MonoBehaviorSingleton<CustomCursorController>
{
    public enum CursorKind
    {
        Default,
        Invalid,
        Pickaxe,
        BaseballBat,
        Axe,
        Shovel
    }

    [Header("Cursor Sprites")]
    [SerializeField] private Sprite defaultCursor;
    [SerializeField] private Sprite invalidCursor;
    [SerializeField] private Sprite pickaxeCursor;
    [SerializeField] private Sprite baseballBatCursor;
    [SerializeField] private Sprite axeCursor;
    [SerializeField] private Sprite shovelCursor;

    [Header("Settings")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;
    [Tooltip("Windows 하드웨어 커서(Auto)는 32px 제한. 큰 커서는 ForceSoftware 권장.")]
    [SerializeField] private CursorMode cursorMode = CursorMode.ForceSoftware;
    [SerializeField] private float worldRaycastDistance = 100f;
    [SerializeField] private LayerMask worldRaycastMask = Physics.DefaultRaycastLayers;

    readonly Dictionary<CursorKind, Texture2D> _textures = new();
    readonly Dictionary<CursorKind, Vector2> _hotspots = new();
    static Material _spriteBlitMaterial;
    static readonly List<RaycastResult> RaycastResults = new();

    CursorKind _appliedKind = (CursorKind)(-1);
    CursorKind? _overrideKind;
    Coroutine _overrideCoroutine;

    Player _cachedPlayer;
    Scene _cachedScene;

    protected override void Awake()
    {
        base.Awake();
        CacheTextures();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshSceneCache();
        ApplyKind(CursorKind.Default);
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshSceneCache();

    void Update()
    {
        ApplyKind(_overrideKind ?? ResolveCursorKind());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            _appliedKind = (CursorKind)(-1);
    }

    /// <summary>자동 판정 대신 특정 커서를 고정합니다.</summary>
    public void SetOverride(CursorKind kind)
    {
        if (_overrideCoroutine != null)
        {
            StopCoroutine(_overrideCoroutine);
            _overrideCoroutine = null;
        }

        _overrideKind = kind;
    }

    /// <summary>고정 커서를 해제하고 자동 판정으로 돌아갑니다.</summary>
    public void ClearOverride()
    {
        if (_overrideCoroutine != null)
        {
            StopCoroutine(_overrideCoroutine);
            _overrideCoroutine = null;
        }

        _overrideKind = null;
    }

    /// <summary>짧은 시간 동안 X 커서를 표시합니다.</summary>
    public void FlashInvalid(float duration = 0.4f)
    {
        if (_overrideCoroutine != null)
            StopCoroutine(_overrideCoroutine);

        _overrideCoroutine = StartCoroutine(CoTemporaryOverride(CursorKind.Invalid, duration));
    }

    IEnumerator CoTemporaryOverride(CursorKind kind, float duration)
    {
        _overrideKind = kind;
        yield return new WaitForSeconds(duration);
        _overrideKind = null;
        _overrideCoroutine = null;
    }

    CursorKind ResolveCursorKind()
    {
        if (TryResolveFromUi(out CursorKind uiKind))
            return uiKind;

        if (TryResolveFromGameplay(out CursorKind gameplayKind))
            return gameplayKind;

        return CursorKind.Default;
    }

    bool TryResolveFromUi(out CursorKind kind)
    {
        kind = CursorKind.Default;

        if (EventSystem.current == null || !TryRaycastUi(out GameObject topHit))
            return false;

        if (TryGetDisabledSelectable(topHit, out kind))
            return true;

        if (TryGetBlockedStageSelectTarget(topHit, out kind))
            return true;

        return false;
    }

    bool TryResolveFromGameplay(out CursorKind kind)
    {
        kind = CursorKind.Default;

        if (InputManager.IsGameplaySuppressed)
            return false;

        RefreshSceneCache();
        if (_cachedPlayer == null)
            return false;

        if (EventSystem.current != null && TryRaycastUi(out GameObject topHit) && UiBlocksGameplayCursor(topHit))
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(GetMouseScreenPosition());
        if (!Physics.Raycast(ray, out RaycastHit hit, worldRaycastDistance, worldRaycastMask, QueryTriggerInteraction.Collide))
            return false;

        if (!TryGetHoverTarget(hit.collider, out HoverTarget target))
            return false;

        if (target.IsPlayer && IsLocalPlayerCollider(hit.collider, _cachedPlayer))
            target.IsPlayer = false;

        if (!target.IsToolTarget && !target.IsPlayer)
            return false;

        if (!_cachedPlayer.isPlayerGetSomething || _cachedPlayer.playerItemSystem?.currentEquipItem == null)
            return false;

        HeldTool tool = GetHeldTool(_cachedPlayer.playerItemSystem.currentEquipItem);
        if (!tool.HasAny)
            return false;

        if (tool.HasPickaxe && target.IsOre)
        {
            kind = CursorKind.Pickaxe;
            return true;
        }

        if (tool.HasBaseballBat && (target.IsMonster || target.IsPlayer))
        {
            kind = CursorKind.BaseballBat;
            return true;
        }

        if (tool.HasAxe && target.IsTree)
        {
            kind = CursorKind.Axe;
            return true;
        }

        if (tool.HasShovel && target.IsTreasure)
        {
            kind = CursorKind.Shovel;
            return true;
        }

        if (IsWrongToolForTarget(tool, target))
        {
            kind = CursorKind.Invalid;
            return true;
        }

        return false;
    }

    struct HoverTarget
    {
        public bool IsOre;
        public bool IsTree;
        public bool IsTreasure;
        public bool IsMonster;
        public bool IsPlayer;

        public bool IsToolTarget => IsOre || IsTree || IsTreasure || IsMonster;
    }

    struct HeldTool
    {
        public bool HasPickaxe;
        public bool HasBaseballBat;
        public bool HasAxe;
        public bool HasShovel;

        public bool HasAny => HasPickaxe || HasBaseballBat || HasAxe || HasShovel;
    }

    static HeldTool GetHeldTool(GameObject heldItem)
    {
        return new HeldTool
        {
            HasPickaxe = heldItem.GetComponent<Pickaxe>() != null,
            HasBaseballBat = heldItem.GetComponent<BaseballBat>() != null,
            HasAxe = heldItem.GetComponent<Axe>() != null,
            HasShovel = heldItem.GetComponent<Shovel>() != null
        };
    }

    static bool IsWrongToolForTarget(HeldTool tool, HoverTarget target)
    {
        if (!tool.HasAny || !target.IsToolTarget)
            return false;

        if (target.IsOre && !tool.HasPickaxe)
            return true;

        if (target.IsTree && !tool.HasAxe)
            return true;

        if (target.IsTreasure && !tool.HasShovel)
            return true;

        if (target.IsMonster && !tool.HasBaseballBat)
            return true;

        return false;
    }

    static bool TryGetHoverTarget(Collider col, out HoverTarget target)
    {
        target = default;
        if (col == null)
            return false;

        if (col.CompareTag(Define.Tag.ORE) || col.GetComponentInParent<Ore>() != null)
            target.IsOre = true;

        if (col.CompareTag(Define.Tag.TREE) || col.GetComponentInParent<TreeResource>() != null)
            target.IsTree = true;

        if (col.CompareTag(Define.Tag.TREASURE_TROVE) || col.GetComponentInParent<TreasureResource>() != null)
            target.IsTreasure = true;

        if (col.CompareTag(Define.Tag.MONSTER) || col.GetComponentInParent<Monster>() != null)
            target.IsMonster = true;

        if (col.CompareTag(Define.Tag.PLAYER) || col.GetComponentInParent<Player>() != null || col.GetComponentInParent<OtherPlayers>() != null)
            target.IsPlayer = true;

        return target.IsToolTarget || target.IsPlayer;
    }

    static bool IsLocalPlayerCollider(Collider col, Player localPlayer)
    {
        if (col == null || localPlayer == null)
            return false;

        return col.transform == localPlayer.transform
               || col.transform.IsChildOf(localPlayer.transform);
    }

    static bool TryGetDisabledSelectable(GameObject hit, out CursorKind kind)
    {
        kind = CursorKind.Default;
        if (hit == null)
            return false;

        Selectable selectable = hit.GetComponentInParent<Selectable>();
        if (selectable != null && !selectable.IsInteractable())
        {
            kind = CursorKind.Invalid;
            return true;
        }

        return false;
    }

    static bool TryGetBlockedStageSelectTarget(GameObject hit, out CursorKind kind)
    {
        kind = CursorKind.Default;
        if (hit == null || SceneManager.GetActiveScene().name != Define.Scene.STAGE_SELECT)
            return false;

        StageManager stageManager = StageManager.Instance;
        if (stageManager == null)
            return false;

        bool isStageTarget = hit.GetComponentInParent<StageNode>() != null
                             || hit.GetComponentInParent<OrbitLineRenderer>() != null;
        if (!isStageTarget)
            return false;

        if (!stageManager.CanInteractWithStagePlanets()
            || stageManager.IsStagePauseMenuOpen)
        {
            kind = CursorKind.Invalid;
            return true;
        }

        return false;
    }

    static bool UiBlocksGameplayCursor(GameObject hit)
    {
        if (hit == null)
            return false;

        Graphic graphic = hit.GetComponentInParent<Graphic>();
        if (graphic == null || !graphic.raycastTarget)
            return false;

        if (hit.GetComponentInParent<Selectable>() != null)
            return true;

        if (hit.GetComponentInParent<IPointerClickHandler>() != null)
            return true;

        return false;
    }

    static bool TryRaycastUi(out GameObject topHit)
    {
        topHit = null;
        if (EventSystem.current == null)
            return false;

        RaycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = GetMouseScreenPosition()
        };
        EventSystem.current.RaycastAll(eventData, RaycastResults);
        if (RaycastResults.Count == 0)
            return false;

        topHit = RaycastResults[0].gameObject;
        return topHit != null;
    }

    static Vector2 GetMouseScreenPosition()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Input.mousePosition;
    }

    void RefreshSceneCache()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene == _cachedScene)
            return;

        _cachedScene = scene;
        _cachedPlayer = FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);
    }

    void CacheTextures()
    {
        _textures.Clear();
        _hotspots.Clear();
        TryCache(CursorKind.Default, defaultCursor);
        TryCache(CursorKind.Invalid, invalidCursor);
        TryCache(CursorKind.Pickaxe, pickaxeCursor);
        TryCache(CursorKind.BaseballBat, baseballBatCursor);
        TryCache(CursorKind.Axe, axeCursor);
        TryCache(CursorKind.Shovel, shovelCursor);
    }

    void TryCache(CursorKind kind, Sprite sprite)
    {
        if (sprite == null)
            return;

        if (!CreateCursorTexture(sprite, out Texture2D texture, out Vector2 scaledHotspot))
            return;

        _textures[kind] = texture;
        _hotspots[kind] = scaledHotspot;
    }

    void ApplyKind(CursorKind kind)
    {
        if (_appliedKind == kind)
            return;

        _appliedKind = kind;

        if (!_textures.TryGetValue(kind, out Texture2D texture) || texture == null)
        {
            if (kind != CursorKind.Default && _textures.TryGetValue(CursorKind.Default, out Texture2D fallback))
            {
                texture = fallback;
                kind = CursorKind.Default;
            }
            else
                return;
        }

        Vector2 hs = _hotspots.TryGetValue(kind, out Vector2 cachedHotspot) ? cachedHotspot : hotspot;
        Cursor.SetCursor(texture, hs, cursorMode);
    }

    bool CreateCursorTexture(Sprite sprite, out Texture2D texture, out Vector2 scaledHotspot)
    {
        texture = null;
        scaledHotspot = hotspot;

        var rect = sprite.textureRect;
        int width = Mathf.Max(1, (int)rect.width);
        int height = Mathf.Max(1, (int)rect.height);

        Texture2D source = ExtractSpriteRegion(sprite);
        if (source == null)
            return false;

        texture = ScaleToCursorSize(source, cursorMode, out float uniformScale);
        scaledHotspot = hotspot * uniformScale;
        return texture != null;
    }

    static Texture2D ExtractSpriteRegion(Sprite sprite)
    {
        var rect = sprite.textureRect;
        int w = Mathf.Max(1, (int)rect.width);
        int h = Mathf.Max(1, (int)rect.height);
        Texture2D atlas = sprite.texture;

        if (atlas != null && atlas.isReadable)
        {
            try
            {
                var readable = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                readable.SetPixels(atlas.GetPixels((int)rect.x, (int)rect.y, w, h));
                readable.Apply();
                return readable;
            }
            catch (UnityException e)
            {
                Debug.LogWarning($"[CustomCursor] GetPixels 실패, RenderTexture로 재시도: {e.Message}");
            }
        }

        return CopySpriteViaRenderTexture(sprite);
    }

    static Texture2D CopySpriteViaRenderTexture(Sprite sprite)
    {
        Texture2D atlas = sprite.texture;
        if (atlas == null)
            return null;

        var rect = sprite.textureRect;
        int w = Mathf.Max(1, (int)rect.width);
        int h = Mathf.Max(1, (int)rect.height);

        Material mat = GetSpriteBlitMaterial();
        if (mat == null)
        {
            Debug.LogWarning("[CustomCursor] Sprites/Default 셰이더를 찾을 수 없습니다.");
            return null;
        }

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;

        float x = rect.x / atlas.width;
        float y = rect.y / atlas.height;
        float uw = rect.width / atlas.width;
        float uh = rect.height / atlas.height;

        mat.mainTexture = atlas;
        mat.SetTextureOffset("_MainTex", new Vector2(x, y));
        mat.SetTextureScale("_MainTex", new Vector2(uw, uh));
        Graphics.Blit(atlas, rt, mat);
        mat.SetTextureOffset("_MainTex", Vector2.zero);
        mat.SetTextureScale("_MainTex", Vector2.one);

        RenderTexture.active = rt;
        var result = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    static Material GetSpriteBlitMaterial()
    {
        if (_spriteBlitMaterial != null)
            return _spriteBlitMaterial;

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
        if (shader == null)
            return null;

        _spriteBlitMaterial = new Material(shader);
        return _spriteBlitMaterial;
    }

    static int GetTargetCursorPixelSize(CursorMode mode)
    {
        int hardCap = mode == CursorMode.ForceSoftware ? 64 : 32;
        const int baseSize = 32;
        if (Screen.dpi <= 0f)
            return Mathf.Min(baseSize, hardCap);

        int scaled = Mathf.RoundToInt(baseSize * Screen.dpi / 96f);
        return Mathf.Clamp(scaled, 24, hardCap);
    }

    static Texture2D ScaleToCursorSize(Texture2D source, CursorMode mode, out float uniformScale)
    {
        uniformScale = 1f;
        int maxDim = GetTargetCursorPixelSize(mode);
        int w = source.width;
        int h = source.height;
        int longest = Mathf.Max(w, h);

        if (longest <= maxDim)
            return source;

        uniformScale = (float)maxDim / longest;
        int newW = Mathf.Max(1, Mathf.RoundToInt(w * uniformScale));
        int newH = Mathf.Max(1, Mathf.RoundToInt(h * uniformScale));

        var dstRt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(source, dstRt);

        RenderTexture.active = dstRt;
        var scaled = new Texture2D(newW, newH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        scaled.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
        scaled.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(dstRt);
        Object.Destroy(source);
        return scaled;
    }
}
