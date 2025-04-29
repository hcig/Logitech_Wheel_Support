using UnityEngine;

namespace HCIG {

    /// <summary>
    /// Used to create Singleton Managers like UIManager
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> {

        private static T _instance;

        public static T Instance {
            get {
                if (_instance == null) {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance == null) {
                        Debug.LogWarning("No " + typeof(T) + " Singleton found in the scene.");
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(this);
            } else {
                _instance = this as T;
            }
        }

        protected virtual void OnDestroy() {
            if(_instance != null && _instance == this) {
                _instance = null;
            }
        }
    }
}