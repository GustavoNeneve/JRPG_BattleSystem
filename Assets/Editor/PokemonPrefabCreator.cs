using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using NewBark.Data; // Assuming SpecieData is here or we parse manually if namespace issues arise.
using NewBark.Support; // For UniquePersistent if needed, though likely not for prefab gen.

public class PokemonPrefabCreator : EditorWindow
{
    private string jsonPath = "Assets/novo projeto/Data/Studio/pokemon";
    private string spritePath = "Assets/novo projeto/graphics/pokedex/pokefront";
    private string templatePath = "Assets/Resources/Enemy_1.prefab";
    private string outputPath = "Assets/Resources/Enemies";

    [MenuItem("NewBark/Create Pokemon Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PokemonPrefabCreator>("Pokemon Prefab Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Pokemon Prefab Generator", EditorStyles.boldLabel);

        jsonPath = EditorGUILayout.TextField("JSON Path", jsonPath);
        spritePath = EditorGUILayout.TextField("Sprite Path (Front)", spritePath);
        templatePath = EditorGUILayout.TextField("Template Prefab Path", templatePath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        if (GUILayout.Button("Generate All Prefabs"))
        {
            GeneratePrefabs();
        }
    }

    private void GeneratePrefabs()
    {
        if (!Directory.Exists(jsonPath))
        {
            Debug.LogError("JSON Path not found: " + jsonPath);
            return;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
        if (template == null)
        {
            Debug.LogError("Template Prefab not found at: " + templatePath);
            return;
        }

        string[] files = Directory.GetFiles(jsonPath, "*.json");
        int count = 0;

        // Whitelist for Trainer_0
        HashSet<string> whitelist = new HashSet<string>() { "ariados", "raticate" };

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string file in files)
            {
                string json = File.ReadAllText(file);
                // Simple parsing to avoid complex dependency if SpecieData fails, 
                // but let's try to infer structure. 
                // We need 'id' and 'dbSymbol'.

                // Using a light wrapper to extract just what we need if full parsing fails
                SimpleSpecieData data = JsonUtility.FromJson<SimpleSpecieData>(json);

                if (data == null || string.IsNullOrEmpty(data.dbSymbol)) continue;

                // Filter check
                if (!whitelist.Contains(data.dbSymbol)) continue;

                string spriteName = data.id.ToString("D4"); // Format 0001, 0168, etc.
                string spriteFullPath = Path.Combine(spritePath, spriteName + ".png");

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

                if (sprite == null)
                {
                    // Try looking for files that start with the ID (e.g. 0168_ariados.png or just 0168.png)
                    // But user confirmed 0168.png exists.
                    // Let's tolerate missing sprites but warn.
                    Debug.LogWarning($"Sprite not found for {data.dbSymbol} (ID: {spriteName}) at {spriteFullPath}");
                    // Continue anyway to create the prefab (placeholders are better than crash)
                }

                // Create Instance
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
                instance.name = data.dbSymbol;

                // Set Sprite
                // Use GetComponentInChildren because the SpriteRenderer might be on a child object (e.g. 'MainContent' or similar)
                var sr = instance.GetComponentInChildren<SpriteRenderer>();

                if (sr != null)
                {
                    if (sprite != null)
                    {
                        sr.sprite = sprite;
                        // Debug.Log($"Assigned sprite {sprite.name} to {instance.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"SpriteRenderer component not found on {instance.name} or children!");
                }

                // Animator is required by CharacterAnimationController, so we keep it.
                // If it overrides the sprite, we might need a workaround later (like an empty controller).
                // For now, we restore it to prevent crashes.

                // Save as Prefab
                string prefabPath = Path.Combine(outputPath, data.dbSymbol + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

                // Destroy Instance
                DestroyImmediate(instance);

                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Finished! Generated {count} prefabs in {outputPath}.");
    }

    [System.Serializable]
    private class SimpleSpecieData
    {
        public int id;
        public string dbSymbol;
    }
}
