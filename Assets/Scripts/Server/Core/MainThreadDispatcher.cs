using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviorSingleton<MainThreadDispatcher>
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    public static void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        // Unity 메인 스레드에서 큐에 쌓인 작업 실행
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue()?.Invoke();
            }
        }
    }
}
