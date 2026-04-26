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
                instance = FindFirstObjectByType<T>();
            
                if (instance == null) 
                {
                    Debug.LogWarning($"Instance of {typeof(T).Name} not found. Creating a new instance.");
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
}
