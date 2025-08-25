using UnityEngine;

namespace UCTools_Utilities
{
    /// <summary>
    /// A simple Singleton pattern implementation for MonoBehaviours.
    /// Inherit from this class to make your MonoBehaviour a singleton.
    /// </summary>
    /// <typeparam name="T">The type of the MonoBehaviour</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// The static instance of this singleton.
        /// </summary>
        private static T _instance;

        /// <summary>
        /// Global access point for this singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                // If the instance doesn't exist yet, try to find it in the scene
                if (_instance == null)
                {
                    Debug.Log("Singleton was null");

                    _instance = FindFirstObjectByType<T>();

                    // If it doesn't exist in the scene, create a new GameObject with it
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }
        
        /// <summary>
        /// Called when the MonoBehaviour is created.
        /// </summary>
        protected virtual void Awake()
        {
            // If an instance already exists that is not this one
            if (_instance != null && _instance != this)
            {
                // Destroy this instance
                Destroy(gameObject);
                return;
            }

            // This is the first/only instance, so make it the singleton
            _instance = this as T;

            
            DontDestroyOnLoad(gameObject);
            

            // Call custom initialization
            OnAwake();
        }

        /// <summary>
        /// Override this method for initialization code instead of Awake.
        /// </summary>
        protected virtual void OnAwake()
        {
        }
    }
}