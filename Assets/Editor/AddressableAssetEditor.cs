using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Events;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

#region Mobile
#if !UNITY_MOBILE_MODE
public class AddressableAssetEditor : EditorWindow {
    private string selectedFolderPath = "Assets/";

    [MenuItem("Tools/Addressable Asset Editor")]
    public static void ShowWindow()
    {
        GetWindow<AddressableAssetEditor>("Addressable Asset Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Select Folder for Addressables", EditorStyles.boldLabel);

        if (GUILayout.Button("Select Folder"))
        {
            selectedFolderPath = EditorUtility.OpenFolderPanel("Select Folder", selectedFolderPath, "");
            // Unity uses assets path related to the project folder, so we need to convert it
            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                selectedFolderPath = "Assets" + selectedFolderPath.Split(new string[] { "/Assets" }, System.StringSplitOptions.None)[1];
                Debug.Log("Selected Folder: " + selectedFolderPath);
            }
        }

        if (GUILayout.Button("Make Addressable and Add Components"))
        {
            MakeAssetsAddressableAndAddComponents(selectedFolderPath);
        }
    }
    private void MakeAssetsAddressableAndAddComponents(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            // Check if the asset is a Model Prefab
            var importer = AssetImporter.GetAtPath(path);
            if (importer is ModelImporter)
            {
                // It's a model prefab, create a variant and add components
                CreatePrefabVariantAndAddComponents(path);
                continue; // Skip the rest of the loop for this asset
            }

            // For regular prefabs and GameObjects, continue as before
            if (PrefabUtility.IsPartOfPrefabAsset(asset))
            {
                GameObject prefabInstance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                AddComponentIfMissing<MeshRenderer>(prefabInstance);
                AddComponentIfMissing<MeshFilter>(prefabInstance);
                AddComponentIfMissing<CapsuleCollider>(prefabInstance);
                AddComponentIfMissing<SynchronousObject>(prefabInstance);

                // Apply changes if it's a prefab
                PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                GameObject.DestroyImmediate(prefabInstance);
            }
            else
            {
                // Handle non-prefab GameObjects if necessary
            }

            // Mark as addressable
            MarkAssetAsAddressable(path, asset.name);
        }

        AssetDatabase.SaveAssets();
        AddressableAssetSettings.CleanPlayerContent();
        AssetDatabase.Refresh();
    }
    private void MarkAssetAsAddressable(string assetPath, string addressName)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), settings.DefaultGroup);
        entry.address = addressName;
    }

    private void CreatePrefabVariantAndAddComponents(string modelPrefabPath)
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabPath);
        if (modelPrefab == null)
        {
            Debug.LogError("Model prefab not found at path: " + modelPrefabPath);
            return;
        }

        // Instantiate the model prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);

        // Add your desired components
        AddComponentIfMissing<MeshRenderer>(instance);
        AddComponentIfMissing<MeshFilter>(instance);
        AddComponentIfMissing<CapsuleCollider>(instance);
        AddComponentIfMissing<SynchronousObject>(instance);

        // Define a valid path for the prefab variant
        // Ensure the directory for variants exists (e.g., "Assets/PrefabVariants/")
        string directoryPath = "Assets/PrefabVariants/";
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }

        // Construct the path for the new variant within the designated directory
        string fileName = System.IO.Path.GetFileNameWithoutExtension(modelPrefabPath) + "_Variant.prefab";
        string variantPath = System.IO.Path.Combine(directoryPath, fileName);

        // Save the prefab variant
        GameObject variant = PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
        if (variant != null)
        {
            Debug.Log("Created prefab variant: " + variantPath);

            // Mark the variant as addressable
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(variantPath), settings.DefaultGroup);
            entry.address = variant.name;
        }
        else
        {
            Debug.LogError("Failed to create prefab variant at path: " + variantPath);
        }
        if (variant != null)
        {
            CreateButtonForPrefab(variant);
        }
        // Clean up the instantiated model prefab
        GameObject.DestroyImmediate(instance);
    }

    private static void AddComponentIfMissing<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.GetComponent<T>() == null)
        {
            gameObject.AddComponent<T>();
        }
    }


    private void CreateButtonForPrefab(GameObject prefabVariant)
    {
        // Ensure that your Tree Content GameObject is loaded and you have a reference to it
        GameObject treeContent = GameObject.Find("Custom Content");
        if (treeContent == null)
        {
            Debug.LogError("Tree Content GameObject not found in the scene.");
            return;
        }


        GameObject syncScript = GameObject.Find("SyncScript");
        SyncScript syncScriptComponent = syncScript.GetComponent<SyncScript>();
        if (syncScriptComponent == null)
        {
            Debug.LogError("SyncScript component not found in the scene.");
            return;
        }


        // Load the button prefab, assuming you have a default button prefab to clone
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Button.prefab");
        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab not found at the path.");
            return;
        }
        // Instantiate the button under Tree Content
        GameObject buttonInstance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, treeContent.transform);
        buttonInstance.name = "Button" + prefabVariant.name;
        buttonInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = prefabVariant.name;

        // Add onClick event listener
        Button buttonComponent = buttonInstance.GetComponent<Button>();
        if (buttonComponent == null)
        {
            Debug.LogError("Button component not found on button prefab.");
            return;
        }


        //buttonComponent.onClick.AddListener(() => syncScriptComponent.SetObject(prefabVariant.name));

        syncScriptComponent.prefabNameToSet = prefabVariant.name;

        UnityEventTools.AddPersistentListener(buttonComponent.onClick, syncScriptComponent.SetObjectWithName);


        Debug.Log($"Button created for the : {prefabVariant.name}");
    }
}
#endif
#endregion