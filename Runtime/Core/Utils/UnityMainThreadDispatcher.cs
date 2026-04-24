using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTuning.Core.Utils
{
    /// <summary>
    /// Dispatcher to execute code on Unity's main thread.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Find existing instance
                    _instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    
                    // Create new instance if none exists
                    if (_instance == null)
                    {
                        var go = new GameObject("UnityMainThreadDispatcher");
                        _instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            // Execute all queued actions
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    try
                    {
                        _executionQueue.Dequeue().Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UnityMainThreadDispatcher] Error executing action: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("[UnityMainThreadDispatcher] Attempted to enqueue null action");
                return;
            }
            
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }
    }
}

