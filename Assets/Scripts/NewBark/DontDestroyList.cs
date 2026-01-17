using UnityEngine;
using NewBark.Support;

namespace NewBark
{
    public class DontDestroyList : MonoBehaviour
    {
        public GameObject[] objects;
        private void Awake()
        {
            foreach (var obj in objects)
            {
                DontDestroyOnLoad(obj);
                UniquePersistent uniquePersistent = obj.AddComponent<UniquePersistent>();
                uniquePersistent.uniqueID = obj.name;
            }
        }
    }
}