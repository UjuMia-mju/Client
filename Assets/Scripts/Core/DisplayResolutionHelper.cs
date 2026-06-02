using UnityEngine;

/// <summary>
/// 요청 해상도를 현재 디스플레이가 지원하는 범위로 맞춘 뒤 Screen.SetResolution을 적용합니다.
/// FHD보다 작은 노트북 등에서 1920x1080 전체화면을 강제할 때 생기는 오류·깨짐을 줄입니다.
/// </summary>
public static class DisplayResolutionHelper
{
    public static Vector2Int GetDisplayMaxSize()
    {
        Resolution res = Screen.currentResolution;
        int w = res.width > 0 ? res.width : Screen.width;
        int h = res.height > 0 ? res.height : Screen.height;

        if (w <= 0) w = 1920;
        if (h <= 0) h = 1080;

        return new Vector2Int(w, h);
    }

    /// <summary>요청 해상도를 디스플레이 최대 크기 안으로 비율을 유지하며 축소합니다.</summary>
    public static Vector2Int ClampToDisplay(Vector2Int requested)
    {
        Vector2Int max = GetDisplayMaxSize();
        if (requested.x <= max.x && requested.y <= max.y)
            return requested;

        float scale = Mathf.Min((float)max.x / requested.x, (float)max.y / requested.y);
        scale = Mathf.Min(scale, 1f);

        return new Vector2Int(
            Mathf.Max(1, Mathf.RoundToInt(requested.x * scale)),
            Mathf.Max(1, Mathf.RoundToInt(requested.y * scale)));
    }

    public static void ApplyFromSettings(SettingsData data)
    {
        if (data == null)
            return;

        FullScreenMode mode = ResolveFullScreenMode(data.windowModeIndex);
        Vector2Int size = ResolveSize(data.resolutionIndex, mode);
        SetResolution(size.x, size.y, mode);
    }

    public static void ApplyResolution(int resolutionIndex, FullScreenMode mode)
    {
        Vector2Int size = ResolveSize(resolutionIndex, mode);
        SetResolution(size.x, size.y, mode);
    }

    public static void ApplyWindowMode(int windowModeIndex, int resolutionIndex)
    {
        FullScreenMode mode = ResolveFullScreenMode(windowModeIndex);
        if (mode == FullScreenMode.Windowed)
        {
            Vector2Int size = ClampToDisplay(new Vector2Int(1280, 720));
            SetResolution(size.x, size.y, mode);
            return;
        }

        Vector2Int res = ResolveSize(resolutionIndex, mode);
        SetResolution(res.x, res.y, mode);
    }

    static FullScreenMode ResolveFullScreenMode(int windowModeIndex)
    {
        return windowModeIndex switch
        {
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.Windowed,
            _ => FullScreenMode.ExclusiveFullScreen
        };
    }

    static Vector2Int ResolveSize(int resolutionIndex, FullScreenMode mode)
    {
        if (mode == FullScreenMode.Windowed)
            return ClampToDisplay(new Vector2Int(1280, 720));

        int safeIndex = resolutionIndex;
        if (safeIndex < 0 || safeIndex >= Define.Resolution.Count)
            safeIndex = 0;

        return ClampToDisplay(Define.Resolution[safeIndex]);
    }

    static void SetResolution(int width, int height, FullScreenMode mode)
    {
        Vector2Int requested = new Vector2Int(width, height);
        Vector2Int clamped = ClampToDisplay(requested);

        if (clamped != requested)
        {
            Debug.LogWarning(
                $"[DisplayResolution] 요청 {requested.x}x{requested.y} → 디스플레이에 맞춰 {clamped.x}x{clamped.y} 적용");
        }

        Screen.SetResolution(clamped.x, clamped.y, mode);
    }
}
