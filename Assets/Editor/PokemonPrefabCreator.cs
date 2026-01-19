using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using NewBark.Data;

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
                SimpleSpecieData data = JsonUtility.FromJson<SimpleSpecieData>(json);

                if (data == null || string.IsNullOrEmpty(data.dbSymbol)) continue;

                // Filter check
                if (!whitelist.Contains(data.dbSymbol)) continue;

                string spriteName = data.id.ToString("D4"); // Format 0001, 0168, etc.
                string spriteFullPath = Path.Combine(spritePath, spriteName + ".png");

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

                if (sprite == null)
                {
                    Debug.LogWarning($"Sprite not found for {data.dbSymbol} (ID: {spriteName}) at {spriteFullPath}");
                }
                else
                {
                    Debug.Log($"Sprite found for {data.dbSymbol} (ID: {spriteName}) at {spriteFullPath}");
                }

                // Create Instance
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
                instance.name = data.dbSymbol;

                // DIRECTLY find the "Sprite" child object to ensure we get the correct components
                Transform spriteInfoTransform = instance.transform.Find("Sprite");
                SpriteRenderer sr = null;
                Animator anim = null;

                if (spriteInfoTransform != null)
                {
                    sr = spriteInfoTransform.GetComponent<SpriteRenderer>();
                    anim = spriteInfoTransform.GetComponent<Animator>();

                    if (sr != null)
                    {
                        if (sprite != null)
                        {
                            sr.sprite = sprite;
                            // Verification log
                            Debug.Log($"Replacing sprite on {instance.name}/Sprite. New: {sprite.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"SpriteRenderer not found on 'Sprite' child of {instance.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"'Sprite' child object not found in template {instance.name}, falling back to InChildren");
                    sr = instance.GetComponentInChildren<SpriteRenderer>();
                    anim = instance.GetComponentInChildren<Animator>();
                }

                // Animator Override Logic
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    // Create Animations Directory
                    string animFolder = Path.Combine(outputPath, "Animations");
                    if (!Directory.Exists(animFolder)) Directory.CreateDirectory(animFolder);

                    // 1. Create a Static Animation Clip for this Sprite
                    AnimationClip clip = new AnimationClip();
                    clip.frameRate = 12;

                    // Set Sprite Curve
                    EditorCurveBinding curveBinding = new EditorCurveBinding();
                    curveBinding.type = typeof(SpriteRenderer);

                    // IF we found the anim on the "Sprite" object where the renderer is, path is empty.
                    curveBinding.path = "";

                    curveBinding.propertyName = "m_Sprite";

                    ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[1];
                    keys[0] = new ObjectReferenceKeyframe();
                    keys[0].time = 0f;
                    keys[0].value = sprite;

                    AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keys);

                    // Save Clip
                    string clipPath = Path.Combine(animFolder, data.dbSymbol + "_idle.anim");
                    AssetDatabase.CreateAsset(clip, clipPath);

                    // 2. Create Override Controller
                    AnimatorOverrideController overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);

                    // Replace ALL clips with our static clip
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    foreach (var originalClip in overrideController.animationClips)
                    {
                        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, clip));
                    }
                    overrideController.ApplyOverrides(overrides);

                    // Save Controller
                    string controllerPath = Path.Combine(animFolder, data.dbSymbol + "_controller.overrideController");
                    AssetDatabase.CreateAsset(overrideController, controllerPath);

                    // Assign
                    anim.runtimeAnimatorController = overrideController;

                    // Disable Animator by default so the Editor Preview uses the SpriteRenderer's static sprite.
                    // IMPORTANT: CharacterAnimationController.Start() must re-enable it!
                    anim.enabled = false;
                }

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
