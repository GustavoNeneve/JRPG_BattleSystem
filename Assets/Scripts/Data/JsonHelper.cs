using System.Collections.Generic;

namespace NewBark.Data
{
    // Helper to wrap lists because Unity JsonUtility doesn't support top-level arrays
    [System.Serializable]
    public class Wrapper<T>
    {
        public List<T> Items;
    }
}
