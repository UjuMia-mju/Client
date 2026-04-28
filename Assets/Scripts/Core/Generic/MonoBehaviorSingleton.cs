using UnityEngine;

public class MonoBehaviorSingleton<T> : MonoBehaviour where T : MonoBehaviorSingleton<T>
{
    private static T instance;
    public static T Instance 
    {
        get 
        {
            if (instance == null) 
            {
                instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            
                if (instance == null) 
                {
                    Debug.LogWarning(
                        $"Instance of {typeof(T).Name} not found. Creating a new instance. " +
                        "Add the component to a scene to avoid this (disabled objects are now found by search).");
                    var obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj); 
                }
            }
            return instance;
        }
    }

    protected virtual void Awake() 
    {
        if (instance == null) 
        {
            instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this) 
        {
            Destroy(this); // gameObject 전체가 아닌 중복 컴포넌트만 제거
        }
        else if (instance == this) 
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>오브젝트가 파괴되면 static instance를 비워, 파괴된 컴포넌트에 캐시가 남는 문제를 막습니다.</summary>
    protected virtual void OnDestroy()
    {
        if (instance == (T)this)
            instance = null;
    }
}
