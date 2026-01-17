using UnityEngine;

namespace NewBark.Support
{
    /// <summary>
    /// Utility for ensuring objects are Unique and Persistent.
    /// Add this to objects that must be Singletons by ID (e.g. Player, Camera) if they are DontDestroyOnLoad.
    /// </summary>
    public class UniquePersistent : MonoBehaviour
    {
        [Tooltip("Unique ID to identify this object. If another object with same ID exists, the new one is destroyed.")]
        public string uniqueID;

        static System.Collections.Generic.Dictionary<string, UniquePersistent> instances = new System.Collections.Generic.Dictionary<string, UniquePersistent>();

        private void Awake()
        {
            if (string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = gameObject.name;
            }

            if (instances.ContainsKey(uniqueID))
            {
                var existing = instances[uniqueID];
                if (existing != null && existing != this)
                {
                    // If existing object is different, destroy THIS (the new one)
                    Destroy(gameObject);
                    return;
                }
                else if (existing == null)
                {
                    instances.Remove(uniqueID);
                }
            }

            if (!instances.ContainsKey(uniqueID))
            {
                instances.Add(uniqueID, this);
                DontDestroyOnLoad(gameObject);

            }
        }
    }
}
