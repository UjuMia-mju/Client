using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 서버 S_STAGE_INFO 기반 스테이지 메타 캐시.
/// 데이터는 정적 보관 — MonoBehaviour 싱글톤이 파괴/재생성돼도 목록이 사라지지 않게 함.
/// </summary>
public class DbCacheManager : MonoBehaviorSingleton<DbCacheManager>
{
    private static readonly List<StageInfo> s_stageInfos = new List<StageInfo>();
    private static readonly Dictionary<(int mapId, int chapter, int stage), StageInfo> s_stageInfoByKey
        = new Dictionary<(int mapId, int chapter, int stage), StageInfo>();

    public static bool HasStageInfo => s_stageInfos.Count > 0;

    public static IReadOnlyList<StageInfo> StageInfos => s_stageInfos;

    public static void RequestDbData()
    {
        PacketDispatcher.Instance.SendGetDbData();
    }

    /// <summary>S_STAGE_INFO 수신 시 호출. 인스턴스 없이도 동작.</summary>
    public static void CacheStageInfos(IEnumerable<StageInfo> stages)
    {
        s_stageInfos.Clear();
        s_stageInfoByKey.Clear();

        if (stages == null)
        {
            Debug.LogWarning("[DbCacheManager] CacheStageInfos: stages가 null입니다.");
            return;
        }

        foreach (StageInfo stage in stages)
        {
            if (stage == null)
                continue;

            s_stageInfos.Add(stage);
            s_stageInfoByKey[(stage.MapId, stage.Chapter, stage.Stage)] = stage;
        }

        Debug.Log($"[DbCacheManager] StageInfo 캐시 전체 갱신(정적): {s_stageInfos.Count}개");
    }

    /// <summary>
    /// 서버 목록이 없을 때 로컬 폴백 1건만 넣을 때 사용. (전체 replace 아님)
    /// </summary>
    public static void MergeStageInfoEntry(StageInfo stage)
    {
        if (stage == null) return;

        for (int i = s_stageInfos.Count - 1; i >= 0; i--)
        {
            StageInfo s = s_stageInfos[i];
            if (s == null) continue;
            if (s.Chapter == stage.Chapter && s.Stage == stage.Stage)
            {
                s_stageInfos.RemoveAt(i);
                s_stageInfoByKey.Remove((s.MapId, s.Chapter, s.Stage));
                break;
            }
        }

        s_stageInfos.Add(stage);
        s_stageInfoByKey[(stage.MapId, stage.Chapter, stage.Stage)] = stage;
        Debug.Log($"[DbCacheManager] 항목 병합(로컬/단건): MapId={stage.MapId} Chapter={stage.Chapter} Stage={stage.Stage}");
    }

    public static void ClearStageCache()
    {
        s_stageInfos.Clear();
        s_stageInfoByKey.Clear();
        Debug.Log("[DbCacheManager] 스테이지 캐시 비움");
    }

    public static bool TryGetStageInfo(int mapId, int chapter, int stage, out StageInfo stageInfo)
    {
        return s_stageInfoByKey.TryGetValue((mapId, chapter, stage), out stageInfo);
    }

    public static bool TryGetStageInfoByChapterStage(int chapter, int stage, out StageInfo stageInfo)
    {
        foreach (StageInfo info in s_stageInfos)
        {
            if (info == null)
                continue;

            if (info.Chapter == chapter && info.Stage == stage)
            {
                stageInfo = info;
                return true;
            }
        }

        stageInfo = null;
        return false;
    }

    /// <summary>S_HOST_SHOW_STAGE 등 map_id만으로 찾을 때(첫 일치).</summary>
    public static bool TryGetStageInfoByMapId(int mapId, out StageInfo stageInfo)
    {
        foreach (StageInfo info in s_stageInfos)
        {
            if (info == null) continue;
            if (info.MapId == mapId)
            {
                stageInfo = info;
                return true;
            }
        }

        stageInfo = null;
        return false;
    }

    public static string BuildChapterStageListDebugString()
    {
        if (s_stageInfos.Count == 0)
            return "캐시 없음(아직 S_STAGE_INFO를 못 받았거나 목록이 비어 있음)";

        var parts = new List<string>(s_stageInfos.Count);
        for (int i = 0; i < s_stageInfos.Count; i++)
        {
            var s = s_stageInfos[i];
            if (s != null)
                parts.Add($"({s.Chapter},{s.Stage})");
        }
        return string.Join(", ", parts);
    }
}
