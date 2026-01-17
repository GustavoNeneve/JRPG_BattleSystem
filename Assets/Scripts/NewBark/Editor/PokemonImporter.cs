using UnityEngine;
using UnityEditor;
using System.IO;
using NewBark.Data;

namespace NewBark.Editor
{
    public class PokemonImporter : AssetPostprocessor
    {
        private const string SOURCE_FOLDER = "Assets/novo projeto/Data/Studio/pokemon";
        private const string DEST_FOLDER = "Assets/novo projeto/Data/PokemonAssets";

        [MenuItem("Tools/Import Pokemon Data")]
        public static void ImportAllData()
        {
            if (!Directory.Exists(DEST_FOLDER))
            {
                Directory.CreateDirectory(DEST_FOLDER);
            }

            // We use standard System.IO for the initial full import to ensure we catch everything
            // regardless of Unity's internal asset database state
            string[] files = Directory.GetFiles(SOURCE_FOLDER, "*.json");
            int importedCount = 0;

            foreach (string file in files)
            {
                // Convert to Unity path for consistency
                string unityPath = file.Replace("\\", "/");
                if (ImportPokemon(unityPath))
                {
                    importedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Successfully imported {importedCount} Pokemon assets to {DEST_FOLDER}");
        }

        // This method is called by Unity whenever assets are imported, deleted, or moved.
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool anyChanged = false;

            foreach (string assetPath in importedAssets)
            {
                // Check if the modified asset is in our source folder and is a JSON file
                if (assetPath.Contains(SOURCE_FOLDER) && assetPath.EndsWith(".json"))
                {
                    ImportPokemon(assetPath);
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Auto-imported updated Pokemon data.");
            }
        }

        private static bool ImportPokemon(string sourceFilePath)
        {
            try
            {
                string json = File.ReadAllText(sourceFilePath);

                // Use temp object to read data first
                PokemonData tempContainer = ScriptableObject.CreateInstance<PokemonData>();
                JsonUtility.FromJsonOverwrite(json, tempContainer);

                if (string.IsNullOrEmpty(tempContainer.dbSymbol))
                {
                    // Fallback to filename if dbSymbol is missing, or skip? 
                    // Let's assume filename is safer if dbSymbol is missing logic-wise for naming the asset
                    tempContainer.dbSymbol = Path.GetFileNameWithoutExtension(sourceFilePath);
                }

                // Determine file name based on dbSymbol as per original plan
                string fileName = tempContainer.dbSymbol;
                string destPath = $"{DEST_FOLDER}/{fileName}.asset";

                // Ensure directory exists (in case it was deleted)
                if (!Directory.Exists(DEST_FOLDER))
                {
                    Directory.CreateDirectory(DEST_FOLDER);
                }

                // Load existing or create new
                PokemonData existingAsset = AssetDatabase.LoadAssetAtPath<PokemonData>(destPath);

                if (existingAsset == null)
                {
                    // Create new
                    existingAsset = ScriptableObject.CreateInstance<PokemonData>();
                    AssetDatabase.CreateAsset(existingAsset, destPath);
                }

                // Overwrite the actual asset with new data
                JsonUtility.FromJsonOverwrite(json, existingAsset);
                EditorUtility.SetDirty(existingAsset);

                // Cleanup temp
                Object.DestroyImmediate(tempContainer);

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to auto-import {sourceFilePath}: {e.Message}");
                return false;
            }
        }
    }
}
