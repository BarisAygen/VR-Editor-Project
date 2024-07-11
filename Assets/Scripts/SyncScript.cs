using Firebase.Auth;
using Firebase.Database;
using Google.XR.Cardboard;
using MG_BlocksEngine2.Core;
using MG_BlocksEngine2.Environment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VideoHelper;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Management;

[Serializable]
public class SyncObject {
    public bool nameChanged;
    public string name;
    public string blockCodeXML;
    public bool isScriptable;
    public string addressableKey;
    public string uid;
    public string parentUid;
    public string type;
    public string color;
    public string text;
    public string url;
    public bool isSpeaking;
    public bool isThinking;
    public string speakingText;
    public string thinkingText;
    public string worldId;
    public List<float> positions;
    public List<float> rotations;
    public List<float> scales;

    public override string ToString()
    {
        return $"UID: {uid} - Type: {type} - Color: {color} -  Text: {text} -  Url: {url}- Positions: ({positions[0]}, {positions[1]}, {positions[2]}) - Rotations: ({rotations[0]}, {rotations[1]}, {rotations[2]}, {rotations[3]}) - Scales: ({scales[0]}, {scales[1]}, {scales[2]})";
    }

    public static bool operator ==(SyncObject obj1, SyncObject obj2)
    {
        return obj1.uid == obj2.uid;
    }

    public static bool operator !=(SyncObject obj1, SyncObject obj2)
    {
        return !(obj1 == obj2);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(uid, type);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is SyncObject))
        {
            return false;
        }

        SyncObject other = (SyncObject)obj;

        // Compare the object's state with the other object's state
        return uid == other.uid && type == other.type && color == other.color
            && positions[0] == other.positions[0]
            && positions[1] == other.positions[1]
            && positions[2] == other.positions[2]
            && rotations[0] == other.rotations[0]
            && rotations[0] == other.rotations[1]
            && rotations[0] == other.rotations[2]
            && rotations[0] == other.rotations[3];
    }
}

public class SyncScript : MonoBehaviour {
    private GameObject mainScrollView;
    // private TextMeshProUGUI accountInfoText;
    private string currentAddressableKey;
    [SerializeField] private Dictionary<string, int> objectCounts = new Dictionary<string, int>();
    [SerializeField] private GameObject EditorObject;
    [SerializeField] private GameObject allCanvas;
    [SerializeField] private GameObject characterScrollView;
    [SerializeField] private GameObject animalScrollView;
    [SerializeField] private GameObject natureScrollView;
    [SerializeField] private GameObject buildingScrollView;
    [SerializeField] private GameObject itemScrollView;
    [SerializeField] private GameObject vehicleScrollView;
    [SerializeField] private GameObject terrainScrollView;
    [SerializeField] private GameObject customScrollView;
    [SerializeField] private GameObject buttonCharacterSection;
    [SerializeField] private GameObject buttonAnimalSection;
    [SerializeField] private GameObject buttonNatureSection;
    [SerializeField] private GameObject buttonBuildingSection;
    [SerializeField] private GameObject buttonItemSection;
    [SerializeField] private GameObject buttonVehicleSection;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Canvas libraryCanvas;
    [SerializeField] private Canvas rightClickCanvas;
    [SerializeField] private GameObject blockEngineScrollView1;
    [SerializeField] private GameObject blockEngineScrollView2;
    [SerializeField] private GameObject blockEngineScrollView3;
    [SerializeField] private Canvas helpCanvas;
    [SerializeField] private Image libraryMenu;
    [SerializeField] private Image libraryMenuImage;
    [SerializeField] private BE2_ProgrammingEnv programmingEnv;
    [SerializeField] private BE2_ExecutionManager executionManager;
    public string currentWorldId;
    public string prefabNameToSet;
    public MakeScriptable makeScriptable;

    private static SyncScript instance;
    public static SyncScript Instance
    {
        get
        {
            return instance;
        }
    }

    private IEnumerator CheckAndFixFirebaseStatus()
    {
        var dependencyTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);
        Firebase.DependencyStatus dependencyStatus = dependencyTask.Result;

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            // accountInfoText.text = $"Account Info: {AuthScript.user.Email}";
            SetDatabaseSync();
            yield return new WaitForEndOfFrame();
        }
    }

    private void SetDatabaseSync()
    {
        currentWorldId = PlayerPrefs.GetString("CurrentWorldId", "");
        string userId = AuthScript.user.UserId; // Ensure you have the user's ID
        string path = $"users/{userId}/worlds/{currentWorldId}/objects";

        FirebaseDatabase.DefaultInstance.GetReference(path).ChildAdded += ObjectAdded;
        FirebaseDatabase.DefaultInstance.GetReference(path).ChildRemoved += ObjectRemoved;
        FirebaseDatabase.DefaultInstance.GetReference(path).ChildChanged += ObjectChanged;
    }

    private void ObjectAdded(object sender, ChildChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != "Main") return;

        List<SynchronousObject> syncObjects = new List<SynchronousObject>(FindObjectsOfType<SynchronousObject>());
        DataSnapshot snapshot = e.Snapshot;
        SyncObject newItemToScene = JsonUtility.FromJson<SyncObject>(snapshot.GetRawJsonValue());
        newItemToScene.uid = snapshot.Key;
        newItemToScene.worldId = currentWorldId;

        if (syncObjects.Find(s => s.syncObject == newItemToScene) != null)
        {
            return;
        }

        Vector3 position = new Vector3(newItemToScene.positions[0], newItemToScene.positions[1], newItemToScene.positions[2]);
        Quaternion rotation = new Quaternion(newItemToScene.rotations[0], newItemToScene.rotations[1], newItemToScene.rotations[2], newItemToScene.rotations[3]);
        Vector3 scale = new Vector3(newItemToScene.scales[0], newItemToScene.scales[1], newItemToScene.scales[2]);
        Color color;
        ColorUtility.TryParseHtmlString(newItemToScene.color, out color);

        if (newItemToScene.addressableKey != "blockCode")
        {
            LoadAddressableObject(newItemToScene.addressableKey, newItemToScene, position, rotation, scale, color);
        }
    }

    private void ObjectChanged(object sender, ChildChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            return;
        }

        // Do something with the data in args.Snapshot
        List<SynchronousObject> syncObjects = new(FindObjectsOfType<SynchronousObject>());
        DataSnapshot snapshot = e.Snapshot;
        SyncObject updatedItemOnFirebase = JsonUtility.FromJson<SyncObject>(snapshot.GetRawJsonValue());
        updatedItemOnFirebase.uid = snapshot.Key;

        if (syncObjects.Find(s => s.syncObject.Equals(updatedItemOnFirebase)) != null)
        {
            return;
        }

        SynchronousObject updateThisObject = syncObjects.Find(s => s.syncObject == updatedItemOnFirebase);

        if (updateThisObject != null)
        {
            Vector3 pos = new(
                    updatedItemOnFirebase.positions[0],
                    updatedItemOnFirebase.positions[1],
                    updatedItemOnFirebase.positions[2]
                    );
            Quaternion rotation = new(
                    updatedItemOnFirebase.rotations[0],
                    updatedItemOnFirebase.rotations[1],
                    updatedItemOnFirebase.rotations[2],
                    updatedItemOnFirebase.rotations[3]
                );
            Vector3 scale = new(
                    updatedItemOnFirebase.scales[0],
                    updatedItemOnFirebase.scales[1],
                    updatedItemOnFirebase.scales[2]
                );
            ColorUtility.TryParseHtmlString($"#{updatedItemOnFirebase.color}", out Color color);
            List<Material> materials = new();
            updateThisObject.GetComponent<MeshRenderer>().GetMaterials(materials);
            updateThisObject.transform.SetPositionAndRotation(pos, rotation);
            updateThisObject.transform.localScale = scale;
            materials[0].SetColor("_Color", color);

            if (updateThisObject.transform.childCount == 2)
            {
                if (updateThisObject.transform.GetChild(1).GetComponent<TextMeshPro>().text != null)
                {
                    updateThisObject.transform.GetChild(1).GetComponent<TextMeshPro>().text = updatedItemOnFirebase.text;
                }
            }

            if (updateThisObject.transform.childCount == 5)
            {
                if (updateThisObject.transform.GetChild(4) != null)
                {
                    if (updateThisObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url != null)
                    {
                        updateThisObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url = updatedItemOnFirebase.url;
                    }
                }
            }

            if (updateThisObject.transform.childCount == 7)
            {
                if (updateThisObject.transform.GetChild(6).GetChild(1) != null)
                {
                    if (updateThisObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text != null)
                    {
                        updateThisObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text = updatedItemOnFirebase.url;
                    }
                }
            }

            updateThisObject.syncObject = updatedItemOnFirebase;
        }
    }

    private void ObjectRemoved(object sender, ChildChangedEventArgs e)
    {
        List<SynchronousObject> syncObjects = new(FindObjectsOfType<SynchronousObject>());
        DataSnapshot snapshot = e.Snapshot;
        SyncObject removedItem = JsonUtility.FromJson<SyncObject>(snapshot.GetRawJsonValue());
        removedItem.uid = snapshot.Key;
        SynchronousObject removeThis = syncObjects.Find(s => s.syncObject == removedItem);

        if (removeThis != null)
        {
            Destroy(removeThis.gameObject);
        }
    }

    public static void ClearCurrentWorldData()
    {
        foreach (SynchronousObject obj in FindObjectsOfType<SynchronousObject>())
        {
            Destroy(obj.gameObject);
        }
    }

    void LoadAddressableObject(string key, SyncObject newItemToScene, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle => {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject prefab = handle.Result;
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, position, rotation);
                    SynchronousObject synchronousObject = instance.GetComponent<SynchronousObject>();
                    synchronousObject.syncObject = newItemToScene;
                    instance.gameObject.AddComponent<BE2_TargetObject>();
                    instance.transform.localScale = scale;
                    Renderer renderer = instance.GetComponent<Renderer>();

                    if (!objectCounts.ContainsKey(newItemToScene.addressableKey))
                    {
                        objectCounts[newItemToScene.addressableKey] = 0;
                    }

                    objectCounts[newItemToScene.addressableKey]++;
                    int currentCount = objectCounts[newItemToScene.addressableKey];
                    string objectName = $"{key}_{currentCount}";

                    if (synchronousObject.syncObject.nameChanged != true)
                    {
                        synchronousObject.syncObject.name = objectName;
                        instance.name = objectName;
                    }

                    if (synchronousObject.syncObject.nameChanged == true)
                    {
                        instance.name = synchronousObject.syncObject.name;
                    }

                    if (renderer != null)
                    {
                        renderer.material.color = color;
                    }
                    var SelectedObject = instance;

                    // After instantiating the object, check if it has a parentUid
                    if (!string.IsNullOrEmpty(newItemToScene.parentUid))
                    {
                        // If the parent object is already loaded, set it directly
                        if (loadedObjects.TryGetValue(newItemToScene.parentUid, out GameObject parentObject))
                        {
                            instance.transform.SetParent(parentObject.transform, false);
                        }
                        else
                        {
                            // If the parent object is not yet loaded, store the parent relationship to set later
                            pendingParenting.Add(newItemToScene.uid, newItemToScene.parentUid);
                        }
                    }

                    // Add the new object to the dictionary of loaded objects
                    loadedObjects[newItemToScene.uid] = instance;

                    // Attempt to resolve pending parenting for any objects that were waiting for this one to load
                    ResolvePendingParenting(newItemToScene.uid, instance);
                }
            }
        };
    }

    // Dictionary to hold objects waiting for their parents to be loaded
    private Dictionary<string, string> pendingParenting = new Dictionary<string, string>();

    // Dictionary to track all loaded objects by their UIDs
    private Dictionary<string, GameObject> loadedObjects = new Dictionary<string, GameObject>();

    private void ResolvePendingParenting(string loadedUid, GameObject loadedObject)
    {
        foreach (var item in pendingParenting.Where(kvp => kvp.Value == loadedUid).ToList())
        {
            if (loadedObjects.TryGetValue(item.Key, out GameObject childObject))
            {
                childObject.transform.SetParent(loadedObject.transform, false);
                pendingParenting.Remove(item.Key);  // Remove the resolved item from the pending list
            }
        }
    }

    public void CopyWorldIdToClipboard()
    {
        currentWorldId = PlayerPrefs.GetString("CurrentWorldId", "");
        GUIUtility.systemCopyBuffer = currentWorldId;
    }

    #region Mobile
#if UNITY_MOBILE_MODE
    public static bool VR_Mode = false;

    private void Start()
    {
        mainCanvas.gameObject.SetActive(false);
        libraryCanvas.gameObject.SetActive(false);
        rightClickCanvas.gameObject.SetActive(false);
        helpCanvas.gameObject.SetActive(false);
        Vector3 currentPosition = blockEngineScrollView1.transform.position;
        blockEngineScrollView1.transform.position = new Vector3(currentPosition.x - 3000, currentPosition.y, currentPosition.z);
        currentPosition = blockEngineScrollView2.transform.position;
        blockEngineScrollView2.transform.position = new Vector3(currentPosition.x - 3000, currentPosition.y, currentPosition.z);
        currentPosition = blockEngineScrollView3.transform.position;
        blockEngineScrollView3.transform.position = new Vector3(currentPosition.x - 3000, currentPosition.y, currentPosition.z);

        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        StartCoroutine(InitializeAndLoad());
#if !UNITY_EDITOR
        StartCoroutine(StartXRCoroutine());
#endif
    }

    private IEnumerator InitializeAndLoad()
    {
        // Ensure Firebase is initialized
        yield return CheckAndFixFirebaseStatus();
        yield return new WaitForSeconds(3f);

        // Load block code from Firebase
        yield return StartCoroutine(BlockCodeManager.Instance.LoadBlockCodeFromFirebase());
        yield return new WaitForSeconds(3f);
        Debug.Log("Calling Play method in BE2_ExecutionManager");
        // Play the loaded block code
        executionManager.Play();
        EventSystem.current.SetSelectedGameObject(null);
    }


#if !UNITY_EDITOR
    private void Update()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            if (Api.IsCloseButtonPressed)
            {
                StopXR();
            }
            if (Api.IsGearButtonPressed)
            {
                // None for now
            }
        }
    }
#endif

    public IEnumerator StartXRCoroutine()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();


        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    void StopXR()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Camera.main.ResetAspect();
    }

#endif
    #endregion

    #region Desktop
#if !UNITY_MOBILE_MODE

    private List<SyncObject> scriptableObjects = new List<SyncObject>();
    private Dictionary<string, GameObject> scrollViews = new Dictionary<string, GameObject>();
    private DatabaseReference reference;
    private FirebaseAuth auth;
    private GameObject terrain;
    private float gridSize = 1;
    private bool isLibraryCanvasUp = false;

    void Start()
    {
        StartCoroutine(GetScriptableObjects());

        if (allCanvas != null)
        {
            allCanvas.SetActive(true);
        }

        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(CheckAndFixFirebaseStatus());
        mainScrollView = GameObject.Find("MainScrollView");
        GameObject parentObject = GameObject.Find("Library Canvas");
        // accountInfoText = parentObject.GetComponentInChildren<TextMeshProUGUI>();
        terrain = GameObject.Find("Terrain");
        scrollViews.Add("character", characterScrollView);
        scrollViews.Add("animal", animalScrollView);
        scrollViews.Add("nature", natureScrollView);
        scrollViews.Add("building", buildingScrollView);
        scrollViews.Add("item", itemScrollView);
        scrollViews.Add("vehicle", vehicleScrollView);
        scrollViews.Add("terrain", terrainScrollView);
        scrollViews.Add("custom", customScrollView);
        // StartCoroutine(InitializeWorldData());
    }

    private IEnumerator InitializeWorldData()
    {
        yield return CheckAndFixFirebaseStatus();
        SetDatabaseSync();

        // Ensure that bubbles are created after all objects are loaded
        yield return new WaitForSeconds(3); // Wait for 2 seconds to ensure all objects are loaded
        makeScriptable.InitializeBubblesFromFirebase();
    }

    private string IGNORE_RAYCAST_LAYER = "Ignore Raycast";
    private string DEFAULT_LAYER = "Default";
    private Vector3 newPosition;
    private List<Color> colors = new List<Color>()
    {
        Color.red, Color.green, Color.gray, Color.black, Color.white
    };

    private bool dragging = false;

    public static bool Dragging
    {
        get
        {
            return Instance.dragging;
        }
        set
        {
            Instance.dragging = value;
        }
    }

    private GameObject selectedObject;

    public static GameObject SelectedObject
    {
        get
        {
            return Instance.selectedObject;
        }
        set
        {
            Instance.selectedObject = value;
        }
    }

    private bool moving = false;
    private float objectHeight = 0;
    private int colorIndex = -1;

    // FixedUpdate is called once per frame
    private void FixedUpdate()
    {
        if (dragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                // Check if the ray intersects with a collider with the "GridCell" tag
                if (raycastHit.collider.CompareTag("GridCell"))
                {
                    newPosition = raycastHit.collider.transform.position;
                    selectedObject.transform.position = newPosition;
                }

                else
                {
                    newPosition = raycastHit.point;
                    newPosition = new Vector3(
                        RoundToNearestGrid(newPosition.x),
                        RoundToNearestGrid(newPosition.y),
                        RoundToNearestGrid(newPosition.z)
                        );
                    selectedObject.transform.position = newPosition;
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && dragging)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                SaveToFirebase();
                GameObject clonedObject = Instantiate(selectedObject, selectedObject.transform.position, Quaternion.identity);
                clonedObject.name = selectedObject.name + "_clone";
                clonedObject.layer = LayerMask.NameToLayer(IGNORE_RAYCAST_LAYER);
                SetGameLayerRecursive(clonedObject, IGNORE_RAYCAST_LAYER);
                selectedObject = clonedObject;
                dragging = true;
            }

            else
            {
                // Finalize placement of the current object
                selectedObject.layer = LayerMask.NameToLayer(DEFAULT_LAYER);
                SetGameLayerRecursive(selectedObject, DEFAULT_LAYER);
                dragging = false;
                SaveToFirebase();

                if (moving)
                {
                    UpdateFromFirebase();
                    moving = false;
                }
            }
        }

        if ((Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.C)) && dragging)
        {
            SetNextColorIndex();
            selectedObject.GetComponent<MeshRenderer>().materials[0].SetColor("_Color", colors[colorIndex]);

        }

        if (Input.GetKeyDown(KeyCode.Escape) && dragging)
        {
            Destroy(selectedObject);
            selectedObject = null;
            dragging = false;
            return;
        }
    }

    public IEnumerator GetScriptableObjects()
    {
        // Assume AuthScript.user.UserId holds the current user's ID
        var query = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).OrderByChild("isScriptable").EqualTo(true);
        var task = query.GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result.Exists)
        {
            scriptableObjects.Clear(); // Clear the list before adding new items

            foreach (var child in task.Result.Children)
            {
                var syncObject = JsonUtility.FromJson<SyncObject>(child.GetRawJsonValue());
                scriptableObjects.Add(syncObject);
            }
        }
    }

    private float RoundToNearestGrid(float pos)
    {
        float xDiff = pos % gridSize;
        pos -= xDiff;

        if (xDiff > (gridSize / 2))
        {
            pos += gridSize;
        }

        return pos;
    }

    public void SetPrefabName(string name)
    {
        prefabNameToSet = name;
    }

    public void SetObjectWithName()
    {
        currentAddressableKey = prefabNameToSet;
        Addressables.LoadAssetAsync<GameObject>(prefabNameToSet).Completed += OnAssetLoaded;
    }

    public void SetObject(string addressableKey)
    {
        currentAddressableKey = addressableKey;
        Addressables.LoadAssetAsync<GameObject>(addressableKey).Completed += OnAssetLoaded;
    }

    private void OnAssetLoaded(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject assetPrefab = obj.Result;
            selectedObject = Instantiate(assetPrefab);
            selectedObject.layer = LayerMask.NameToLayer(Instance.IGNORE_RAYCAST_LAYER);
            objectHeight = selectedObject.GetComponent<Renderer>().bounds.size.y;
            SetGameLayerRecursive(selectedObject, Instance.IGNORE_RAYCAST_LAYER);
            dragging = true;
        }
    }

    public static void EditObject(Transform selection)
    {
        Instance.selectedObject = selection.gameObject;
        Instance.selectedObject.layer = LayerMask.NameToLayer(Instance.IGNORE_RAYCAST_LAYER);
        Instance.SetGameLayerRecursive(Instance.selectedObject, Instance.IGNORE_RAYCAST_LAYER);
        Instance.objectHeight = Instance.selectedObject.GetComponent<Renderer>().bounds.size.y;
        Instance.dragging = true;
        Instance.moving = true;

        if (SelectedObject.GetComponent<TextMeshPro>() != null)
        {
            var input = SelectedObject.GetComponent<TextMeshPro>().text;
            Console.WriteLine(input);
        }
    }

    public static void RemoveObject(Transform selection)
    {
        Instance.selectedObject = selection.gameObject;
        Destroy(Instance.selectedObject);
        Instance.RemoveFromFirebase();
    }

    private void SetNextColorIndex()
    {
        if (colorIndex < colors.Count - 1)
        {
            colorIndex++;
        }

        else
        {
            colorIndex = 0;
        }
    }

    private void SetGameLayerRecursive(GameObject _go, string _layer)
    {
        _go.layer = LayerMask.NameToLayer(_layer);

        foreach (Transform child in _go.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer(_layer);
            Transform _HasChildren = child.GetComponentInChildren<Transform>();

            if (_HasChildren != null)
                SetGameLayerRecursive(child.gameObject, _layer);
        }
    }

    public void SaveToFirebase()
    {
        int count = 0;
        if (objectCounts.TryGetValue(currentAddressableKey, out int currentCount))
        {
            count = currentCount;
            objectCounts[currentAddressableKey] = currentCount + 1;
        }
        else
        {
            objectCounts[currentAddressableKey] = 1;
        }

        SyncObject newObject = new SyncObject
        {
            nameChanged = false,
            blockCodeXML = "",
            isScriptable = false,
            addressableKey = currentAddressableKey,
            name = currentAddressableKey + "_" + objectCounts[currentAddressableKey].ToString(),
            type = "no type",
            color = ColorUtility.ToHtmlStringRGBA(selectedObject.GetComponent<MeshRenderer>().materials[0].GetColor("_Color")),
            text = "no text",
            url = "no url",
            isSpeaking = false,
            speakingText = "",
            isThinking = false,
            thinkingText = "",
            positions = new List<float>(),
            rotations = new List<float>(),
            scales = new List<float>()
        };

        newObject.positions.Add(selectedObject.transform.position.x);
        newObject.positions.Add(selectedObject.transform.position.y);
        newObject.positions.Add(selectedObject.transform.position.z);
        newObject.rotations.Add(selectedObject.transform.rotation.x);
        newObject.rotations.Add(selectedObject.transform.rotation.y);
        newObject.rotations.Add(selectedObject.transform.rotation.z);
        newObject.rotations.Add(selectedObject.transform.rotation.w);
        newObject.scales.Add(selectedObject.transform.localScale.x);
        newObject.scales.Add(selectedObject.transform.localScale.y);
        newObject.scales.Add(selectedObject.transform.localScale.z);

        if (Instance.selectedObject.transform.childCount == 2)
        {
            if (Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>() != null)
            {
                if (Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>().text != null)
                {
                    newObject.text = Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>().text;
                }
            }
        }

        if (Instance.selectedObject.transform.childCount == 5)
        {
            if (Instance.selectedObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url != null)
            {
                newObject.url = Instance.selectedObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url;
            }
        }

        if (Instance.selectedObject.transform.childCount == 7)
        {
            if (Instance.selectedObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text != null)
            {
                newObject.url = Instance.selectedObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text;
            }
        }

        string userId = AuthScript.user.UserId;
        string path = $"users/{userId}/worlds/{currentWorldId}/objects";
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference(path);
        string key = reference.Push().Key;
        newObject.uid = key;
        selectedObject.GetComponent<SynchronousObject>().syncObject = newObject;
        reference.Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(newObject));
    }

    public void MakeObjectScriptable(Transform selectedTransform, bool makeScriptable)
    {
        if (selectedTransform != null)
        {
            var syncObjectComponent = selectedTransform.GetComponent<SynchronousObject>();
            if (syncObjectComponent != null)
            {
                syncObjectComponent.syncObject.isScriptable = makeScriptable;
                UpdateFromFirebase(selectedTransform);
            }
        }
    }

    public static void UpdateFromFirebase(Transform selection = null)
    {
        if (selection != null)
        {
            Instance.selectedObject = selection.gameObject;
        }

        SyncObject editedObject = Instance.selectedObject.GetComponent<SynchronousObject>().syncObject;
        editedObject.color = ColorUtility.ToHtmlStringRGBA(Instance.selectedObject.GetComponent<MeshRenderer>().materials[0].GetColor("_Color"));

        if (Instance.selectedObject.transform.childCount == 2)
        {
            if (Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>() != null)
            {
                if (Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>().text != null)
                {
                    editedObject.text = Instance.selectedObject.transform.GetChild(1).GetComponent<TextMeshPro>().text;
                }
            }
        }

        if (Instance.selectedObject.transform.childCount == 5)
        {
            if (Instance.selectedObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url != null)
            {
                editedObject.url = Instance.selectedObject.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<VideoPlayer>().url;
            }
        }

        if (Instance.selectedObject.transform.childCount == 7)
        {
            if (Instance.selectedObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text != null)
            {
                editedObject.url = Instance.selectedObject.transform.GetChild(6).GetChild(1).GetComponent<InputField>().text;
            }
        }

        editedObject.positions = new List<float>
    {
        Instance.selectedObject.transform.position.x,
        Instance.selectedObject.transform.position.y,
        Instance.selectedObject.transform.position.z
    };

        editedObject.rotations = new List<float>
    {
        Instance.selectedObject.transform.rotation.x,
        Instance.selectedObject.transform.rotation.y,
        Instance.selectedObject.transform.rotation.z,
        Instance.selectedObject.transform.rotation.w
    };

        editedObject.scales = new List<float>
    {
        Instance.selectedObject.transform.localScale.x,
        Instance.selectedObject.transform.localScale.y,
        Instance.selectedObject.transform.localScale.z,
    };

        string objectToJson = JsonUtility.ToJson(editedObject);
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).Child("worlds").Child(Instance.currentWorldId).Child("objects");
        reference.Child(editedObject.uid).SetRawJsonValueAsync(objectToJson).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Object updated successfully in Firebase.");
            }
        });
    }


    public void RemoveFromFirebase()
    {
        if (selectedObject != null)
        {
            SyncObject syncObject = selectedObject.GetComponent<SynchronousObject>().syncObject;
            string userId = AuthScript.user.UserId;
            string path = $"users/{userId}/worlds/{currentWorldId}/objects";
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference(path);
            reference.Child(syncObject.uid).RemoveValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Object deleted successfully from Firebase.");
                }
            });
        }
    }

    public void LoadHomeScene()
    {
        SceneManager.LoadScene(1);
    }

    public void ManageHelpCanvas()
    {
        helpCanvas.SetGameObjectActive(!helpCanvas.isActiveAndEnabled);
    }

    public void ToggleLibraryCanvas()
    {
        DeactivateAllScrollViews();
        StartCoroutine(SlideCanvas(libraryMenu.GetComponent<RectTransform>(), isLibraryCanvasUp ? -205f : 205f));
        isLibraryCanvasUp = !isLibraryCanvasUp;
        libraryMenuImage.transform.Rotate(0, 0, 180f);
    }

    private void DeactivateAllScrollViews()
    {
        foreach (var scrollView in Instance.scrollViews.Values)
        {
            scrollView.SetActive(false);
        }
    }

    private IEnumerator SlideCanvas(RectTransform canvasTransform, float slideAmount)
    {
        Vector2 startPosition = canvasTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, slideAmount);
        float timeElapsed = 0;

        while (timeElapsed < 0.5f)
        {
            canvasTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, timeElapsed / 0.5f);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        canvasTransform.anchoredPosition = endPosition;
    }

    public static void ScrollActivation(string scrollViewKey)
    {
        foreach (var key in Instance.scrollViews.Keys)
        {
            if (Instance.scrollViews.TryGetValue(key, out GameObject scrollView))
            {
                scrollView.SetActive(key == scrollViewKey);
            }
        }
    }

    public static void ActivateTrain01()
    {
        if (Instance.terrain.activeSelf == true)
        {
            Instance.terrain.SetActive(false);
        }

        else
        {
            Instance.terrain.SetActive(true);
        }
    }

    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        SceneManager.LoadScene(0); // Assuming your sign-in scene is at index 0
    }

#endif
    #endregion
}