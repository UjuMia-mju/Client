using UnityEngine;

/// <summary>
/// Render Settings 거리 안개 밀도와 Whitish Fog(파티클) 활성 상태를 함께 맞춥니다.
/// 이 오브젝트가 꺼지면 컴포넌트 실행이 멈추므로 외부에서는 <see cref="FindIncludingInactive"/>로 찾은 뒤 호출합니다.
/// </summary>
public class FogController : MonoBehaviour
{
    static FogController Cached;

    const float DensityWhenOff = 0f;

    [Tooltip("켜졌을 때 Rendering > Fog 의 Density")]
    [SerializeField]
    float densityWhenOn = 0.2f;

    void Awake()
    {
        Cached = this;
    }

    void OnDestroy()
    {
        if (Cached == this)
            Cached = null;
    }

    /// <summary>WhitishFog가 비활성이라도 포함해 한 개를 반환합니다. 없으면 null.</summary>
    public static FogController FindIncludingInactive()
    {
        if (Cached != null)
            return Cached;

#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<FogController>(FindObjectsInactive.Include);
#else
        var all = Resources.FindObjectsOfTypeAll<FogController>();
        return all.Length > 0 ? all[0] : null;
#endif
    }

    public bool IsFogEffectivelyOn =>
        RenderSettings.fog && gameObject.activeSelf &&
        RenderSettings.fogDensity > DensityWhenOff;

    public void SetFogEnabled(bool enabled)
    {
        RenderSettings.fogDensity = enabled ? Mathf.Max(0f, densityWhenOn) : DensityWhenOff;
        RenderSettings.fog = enabled;

        if (enabled && !gameObject.activeSelf)
            gameObject.SetActive(true);
        else if (!enabled && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void ToggleFog()
    {
        SetFogEnabled(!IsFogEffectivelyOn);
    }

    /// <inheritdoc cref="SetFogEnabled"/>
    public static void SetFogGlobal(bool enabled)
    {
        var c = FindIncludingInactive();
        if (c != null)
            c.SetFogEnabled(enabled);
    }

    /// <inheritdoc cref="ToggleFog"/>
    public static void ToggleFogGlobal()
    {
        var c = FindIncludingInactive();
        if (c != null)
            c.ToggleFog();
    }
}
