using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PokemonAnimationGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Pokemon Animations")]
    public static void ShowWindow()
    {
        GetWindow<PokemonAnimationGenerator>("Pokemon Anim Gen");
    }

    // Default paths
    string sourcePrefabPath = "Assets/Resources/Player_1 1.prefab";
    string spritesFolder = "Assets/Resources/Pokemon/Back";
    string outputFolder = "Assets/Resources/Pokemon/Animations";

    void OnGUI()
    {
        GUILayout.Label("Generator Settings", EditorStyles.boldLabel);
        sourcePrefabPath = EditorGUILayout.TextField("Source Prefab Path", sourcePrefabPath);
        spritesFolder = EditorGUILayout.TextField("Sprites Folder", spritesFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Generate Animations"))
        {
            Generate();
        }
    }

    void Generate()
    {
        // 1. Load Source Prefab and Controller
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError($"Source Prefab not found at {sourcePrefabPath}");
            return;
        }

        var anim = sourcePrefab.GetComponent<Animator>(); // Or GetComponentInChildren?
        if (anim == null) anim = sourcePrefab.GetComponentInChildren<Animator>();

        if (anim == null || anim.runtimeAnimatorController == null)
        {
            Debug.LogError("Source Prefab does not have an Animator with a Controller!");
            return;
        }

        RuntimeAnimatorController baseController = anim.runtimeAnimatorController;
        
        // Ensure Output Folder
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        // 2. Find all Sprites
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesFolder });
        
        // 3. Process Each Sprite
        foreach (string guid in spriteGuids)
        {
            string spritePath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) continue;

            string id = sprite.name; // e.g. "000"
            
             // Create Subfolder for this ID to avoid clutter? Or flat? User asked for "Assets/Resources/Pokemon/Animations/[ID].controller" likely
             // Let's keep flat for Controllers, maybe clips in subfolder?
             // Let's create a subfolder for the CLIPS, but keep Controller in root of output?
             // Or better: Output/ID/
            
            string idFolder = Path.Combine(outputFolder, id);
            if (!Directory.Exists(idFolder)) Directory.CreateDirectory(idFolder);

            // Create Override Controller
            AnimatorOverrideController overrideCtrl = new AnimatorOverrideController(baseController);
            string ctrlName = $"{id}.controller"; // 000.controller
            string ctrlPath = Path.Combine(idFolder, ctrlName); // Store inside ID folder for organization, or Root? 
            // Logic in CombatManager expects: Resources/Pokemon/Animations/[ID]
            // Resources.Load("Pokemon/Animations/000") implies the file is named 000.controller directly in that folder.
            // So we must put 000.controller directly in OutputFolder.
            ctrlPath = Path.Combine(outputFolder, ctrlName);

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            foreach (var clip in baseController.animationClips)
            {
                // Duplicate Clip
                AnimationClip newClip = new AnimationClip();
                string newClipName = $"{id}_{clip.name}.anim";
                string newClipPath = Path.Combine(idFolder, newClipName);

                // Copy settings
                newClip.frameRate = clip.frameRate;
                newClip.wrapMode = clip.wrapMode;
                // Copy curves/events? 
                // We need to copy raw data. 
                // Since AnimationClip properties are limited in API, simpler to Copy Asset?
                
                // Copy Asset is better to preserve everything
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clip), newClipPath);
                newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newClipPath); // Reload the copy

                // Modify the copy: Replace Sprites
                ReplaceSpriteInClip(newClip, sprite);

                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, newClip));
            }

            overrideCtrl.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(overrideCtrl, ctrlPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generation Complete!");
    }

    void ReplaceSpriteInClip(AnimationClip clip, Sprite newSprite)
    {
        // Get all bindings
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
            {
                // Get Keyframes
                var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                
                // Replace values
                for (int i = 0; i < keyframes.Length; i++)
                {
                    keyframes[i].value = newSprite;
                }

                // Set back
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            }
        }
    }
}
