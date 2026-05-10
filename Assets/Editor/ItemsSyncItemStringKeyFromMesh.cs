#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬에 배치된 <see cref="Items"/>의 <c>itemStringKey</c>를
/// 같은 오브젝트 트리 안 <see cref="MeshFilter"/>의 <c>sharedMesh.name</c>에 맞춥니다.
/// </summary>
public static class ItemsSyncItemStringKeyFromMesh
{
    const string MenuRoot = "Tools/UjuMia/Items/";

    [MenuItem(MenuRoot + "Sync itemStringKey ← MeshFilter mesh name (활성 씬 전체)")]
    static void SyncActiveScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Items 동기화", "유효한 활성 씬이 없습니다.", "확인");
            return;
        }

        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            changed += ProcessHierarchy(root.transform);

        Debug.Log($"[ItemsSync] 활성 씬 '{scene.name}': Items {changed}곳 갱신 (MeshFilter.sharedMesh.name 기준).");
    }

    [MenuItem(MenuRoot + "Sync itemStringKey ← MeshFilter mesh name (선택 오브젝트 하위만)")]
    static void SyncSelection()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Items 동기화", "계층에서 GameObject를 한 개 이상 선택하세요.", "확인");
            return;
        }

        int changed = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null) continue;
            changed += ProcessHierarchy(go.transform);
        }

        Debug.Log($"[ItemsSync] 선택 하위: Items {changed}곳 갱신.");
    }

    /// <returns>변경된 <see cref="Items"/> 개수</returns>
    static int ProcessHierarchy(Transform root)
    {
        int changed = 0;
        var items = root.GetComponentsInChildren<Items>(includeInactive: true);
        foreach (Items item in items)
        {
            if (item == null) continue;
            if (TrySyncOne(item))
                changed++;
        }

        return changed;
    }

    static bool TrySyncOne(Items items)
    {
        if (items == null) return false;

        var mf = items.GetComponentInChildren<MeshFilter>(includeInactive: true);
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning($"[ItemsSync] Mesh 없음 — 건너뜀: '{items.name}' (경로: {GetPath(items.transform)})", items);
            return false;
        }

        string meshName = mf.sharedMesh.name;
        if (string.IsNullOrEmpty(meshName))
        {
            Debug.LogWarning($"[ItemsSync] sharedMesh.name 비어 있음 — 건너뜀: '{items.name}'", items);
            return false;
        }

        if (items.itemStringKey == meshName)
            return false;

        Undo.RecordObject(items, "Sync itemStringKey from mesh name");
        items.itemStringKey = meshName;
        EditorUtility.SetDirty(items);
        Scene sc = items.gameObject.scene;
        if (sc.IsValid() && sc.isLoaded)
            EditorSceneManager.MarkSceneDirty(sc);
        return true;
    }

    static string GetPath(Transform t)
    {
        if (t == null) return "";
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
#endif
