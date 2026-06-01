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
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private float worldRaycastDistance = 100f;
    [SerializeField] private LayerMask worldRaycastMask = Physics.DefaultRaycastLayers;

    readonly Dictionary<CursorKind, Texture2D> _textures = new();
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

        Texture2D texture = CreateTextureFromSprite(sprite);
        if (texture != null)
            _textures[kind] = texture;
    }

    void ApplyKind(CursorKind kind)
    {
        if (_appliedKind == kind)
            return;

        _appliedKind = kind;

        if (!_textures.TryGetValue(kind, out Texture2D texture) || texture == null)
        {
            if (kind != CursorKind.Default && _textures.TryGetValue(CursorKind.Default, out Texture2D fallback))
                texture = fallback;
            else
                return;
        }

        Cursor.SetCursor(texture, hotspot, cursorMode);
    }

    static Texture2D CreateTextureFromSprite(Sprite sprite)
    {
        var rect = sprite.textureRect;
        int width = (int)rect.width;
        int height = (int)rect.height;

        if (width <= 0 || height <= 0)
            return null;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = sprite.texture.filterMode,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
