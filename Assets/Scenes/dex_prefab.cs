using System.Collections.Generic;
using UnityEngine;
using NewBark.Support;
using System.Diagnostics;

public class dex_prefab : MonoBehaviour
{
    public static dex_prefab instance;

    [Header("Enemy Database")]
    public List<GameObject> enemyPrefabs;

    private Dictionary<string, GameObject> enemyLookup = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);

        var uniquePersistent = GetComponent<UniquePersistent>();
        if (uniquePersistent == null)
            uniquePersistent = gameObject.AddComponent<UniquePersistent>();

        uniquePersistent.uniqueID = "dex_prefab";

        InitializeDatabase();
    }

    void InitializeDatabase()
    {
        enemyLookup.Clear();
        foreach (var prefab in enemyPrefabs)
        {
            if (prefab != null)
            {
                // We use the prefab name as Key. 
                // Alternatively, we could look for a specific component with an ID.
                if (!enemyLookup.ContainsKey(prefab.name))
                {
                    enemyLookup.Add(prefab.name, prefab);
                }
            }
        }
    }

    public GameObject GetEnemyPrefab(string enemyName)
    {
        if (enemyLookup.TryGetValue(enemyName, out var prefab))
        {
            return prefab;
        }

        // Use fuzzy search or fallback?
        UnityEngine.Debug.LogWarning($"Enemy {enemyName} not found in dex_prefab!");
        return null; // or default
    }
}
