using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 로그인 전/후 공통으로 사용하는 고정 DB 데이터(스테이지 등) 캐시.
/// </summary>
public class DbCacheManager : MonoBehaviorSingleton<DbCacheManager>
{
    private readonly Dictionary<(int mapId, int chapter, int stage), StageInfo> _stageInfoByKey
        = new Dictionary<(int mapId, int chapter, int stage), StageInfo>();

    private readonly List<StageInfo> _stageInfos = new List<StageInfo>();

    public bool HasStageInfo => _stageInfos.Count > 0;

    public IReadOnlyList<StageInfo> StageInfos => _stageInfos;

    public void RequestDbData()
    {
        PacketDispatcher.Instance.SendGetDbData();
    }

    public void CacheStageInfos(IEnumerable<StageInfo> stages)
    {
        _stageInfos.Clear();
        _stageInfoByKey.Clear();

        if (stages == null)
            return;

        foreach (StageInfo stage in stages)
        {
            if (stage == null)
                continue;

            _stageInfos.Add(stage);
            _stageInfoByKey[(stage.MapId, stage.Chapter, stage.Stage)] = stage;
        }

        Debug.Log($"[DbCacheManager] StageInfo 캐시 완료: {_stageInfos.Count}개");
    }

    public bool TryGetStageInfo(int mapId, int chapter, int stage, out StageInfo stageInfo)
    {
        return _stageInfoByKey.TryGetValue((mapId, chapter, stage), out stageInfo);
    }

    public bool TryGetStageInfoByChapterStage(int chapter, int stage, out StageInfo stageInfo)
    {
        foreach (StageInfo info in _stageInfos)
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
}
