using UnityEngine;

namespace HCIG {

    public class DontDestroyOnLoad : MonoBehaviour {

        /// <summary>
        /// Move to the DontDestroyOnLoad-Handler, so this object never be destroyed when we switch scenes
        /// </summary>
        private void Awake() {
            if (transform.parent != null) {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            Destroy(this);
        }
    }
}
