using Firebase.Database;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[Serializable]
public class WorldData {
    public string name;
    public string creationDate;
    public string worldId;

}

public class WorldManager : MonoBehaviour {

    public Button createWorldButton;
    public Button copyWorldIdButton; // Button to copy world ID
    public Button loadSharedWorldButton; // Button to load shared world
    [SerializeField] private GameObject worldButtonPrefab;
    [SerializeField] private Transform worldListParent;
    [SerializeField] private TMP_InputField newWorldNameInputField;
    [SerializeField] private TMP_InputField sharedWorldIdInputField; // Input field for shared world ID
    public string currentWorldId; // To keep track of the current world ID


    void Start()
    {
        StartCoroutine(CheckAndFixDependenciesThenFetchWorlds());
        if (createWorldButton != null) 
        { 
            createWorldButton.onClick.AddListener(CreateNewWorld);
        }

        if (copyWorldIdButton != null)
        {
            copyWorldIdButton.onClick.AddListener(CopyWorldIdToClipboard);

        }

        if (loadSharedWorldButton != null)
        {
            loadSharedWorldButton.onClick.AddListener(LoadSharedWorld);

        }
    }
    private IEnumerator CheckAndFixDependenciesThenFetchWorlds()
    {
        var dependencyCheck = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyCheck.IsCompleted);

        if (dependencyCheck.Exception != null)
        {
            Debug.LogError("Dependency check failed: " + dependencyCheck.Exception);
            yield break;
        }

        var dependencyStatus = dependencyCheck.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            Debug.Log("Firebase is ready for use.");
            ClearCurrentWorldData(); // Clear existing objects when starting
            FetchAndDisplayWorlds();
        }
        else
        {
            Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
        }
    }

    private void ClearCurrentWorldData()
    {
        SyncScript.Instance.ClearCurrentWorldData();
    }

    private void FetchAndDisplayWorlds()
    {
        string userId = AuthScript.user.UserId;
        FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds").GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error fetching worlds: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                UnityMainThreadDispatcher.Instance().Enqueue(() => SelectWorldButtons(snapshot));
            }
        });
    }

    private void SelectWorldButtons(DataSnapshot snapshot) //this function is created to distinguish the selection and deletion buttons in the world scene because they are in the same path in the hiearachy
    {
        foreach (DataSnapshot worldSnapshot in snapshot.Children)
        {
            var worldData = JsonUtility.FromJson<WorldData>(worldSnapshot.GetRawJsonValue());
            GameObject buttonObj = Instantiate(worldButtonPrefab, worldListParent);

            // Assuming your prefab has child buttons tagged or named appropriately
            Button selectButton = buttonObj.transform.Find("SelectButton").GetComponent<Button>();
            Button deleteButton = buttonObj.transform.Find("DeleteButton").GetComponent<Button>();

            // Set the world name on the select button (assuming it has a Text component as a child)
            selectButton.GetComponentInChildren<TextMeshProUGUI>().text = worldData.name;

            // Add listeners to the buttons
            selectButton.onClick.AddListener(() => SelectWorld(worldData.worldId));
            deleteButton.onClick.AddListener(() => DeleteWorldPrompt(worldData.worldId, buttonObj));
        }
    }

    private void DeleteWorldPrompt(string worldId, GameObject buttonObj)
    {
        // Optional: Show a confirmation dialog before deletion
        Debug.Log($"Prompt to delete world {worldId}");
        // If confirmed:
        DeleteWorld(AuthScript.user.UserId, worldId, buttonObj);
    }

    private void DeleteWorld(string userId, string worldId, GameObject buttonObj)
    {
        DatabaseReference worldRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds/{worldId}");
        DatabaseReference mappingRef = FirebaseDatabase.DefaultInstance.GetReference($"worldMappings/{worldId}");

        // Use multiple asynchronous operations to delete both the world and the mapping
        worldRef.RemoveValueAsync().ContinueWith(worldTask =>
        {
            if (worldTask.IsCompleted)
            {
                mappingRef.RemoveValueAsync().ContinueWith(mappingTask =>
                {
                    if (mappingTask.IsCompleted)
                    {
                        Debug.Log("World deleted: " + worldId);
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Destroy(buttonObj); // Remove the button from the UI
                        });
                    }
                    else
                    {
                        Debug.LogError("Error deleting world mapping: " + mappingTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("Error deleting world: " + worldTask.Exception);
            }
        });
    }

    public void SelectWorld(string worldId)
    {
        Debug.Log("Selected world: " + worldId);
        LoadWorld(AuthScript.user.UserId, worldId);
    }

    public void CreateNewWorld()
    {
        Debug.Log("Creating new world.");
        string userId = AuthScript.user.UserId;
        var newWorldKey = FirebaseDatabase.DefaultInstance.GetReference("users").Child(userId).Child("worlds").Push().Key;
        string worldName = string.IsNullOrEmpty(newWorldNameInputField.text) ? "New World" : newWorldNameInputField.text;

        var worldData = new WorldData
        {
            name = worldName,
            creationDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            worldId = newWorldKey
        };

        DatabaseReference worldRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds/{newWorldKey}");
        DatabaseReference mappingRef = FirebaseDatabase.DefaultInstance.GetReference($"worldMappings/{newWorldKey}");

        worldRef.SetRawJsonValueAsync(JsonUtility.ToJson(worldData)).ContinueWith(worldTask => {
            if (worldTask.IsCompleted)
            {
                mappingRef.SetValueAsync(userId).ContinueWith(mappingTask => {
                    if (mappingTask.IsCompleted)
                    {
                        Debug.Log($"New world {worldData.name} created.");
                        Debug.Log($"New world {worldData.worldId}");
                        Debug.Log($"New world {userId}");
                        LoadWorld(userId, newWorldKey);
                    }
                    else
                    {
                        Debug.LogError("Failed to update mapping: " + mappingTask.Exception);
                        // Optional: Handle the failure to write to the mapping node.
                    }
                });
            }
            else
            {
                Debug.LogError("Failed to create world: " + worldTask.Exception);
                // Optional: Handle the failure to write to the world node.
            }
        });
    }

    public void LoadWorld(string userId, string worldId)
    {
        Debug.Log("Attempting to load world.");
        Debug.Log($"User ID: {userId}, World ID: {worldId}");

        FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds/{worldId}").GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error loading data: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Debug.Log("Data snapshot retrieved successfully.");

                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        var worldData = JsonUtility.FromJson<WorldData>(snapshot.GetRawJsonValue());
                        Debug.Log($"World Data: {worldData}");

                        SyncScript.Instance.InitializeWorld(worldData);
                        Debug.Log("Initializing world in SyncScript.");

                        Debug.Log("Loading the scene 'Main'.");
                        SceneManager.LoadScene("Main");
                    });
                }
                else
                {
                    Debug.LogError("No data found for the specified world ID.");
                }
            }
        });
    }



    public void CopyWorldIdToClipboard()
    {
        currentWorldId = PlayerPrefs.GetString("CurrentWorldId", "");
        GUIUtility.systemCopyBuffer = currentWorldId;
        Debug.Log("World ID copied to clipboard: " + currentWorldId);
    }

    public void LoadSharedWorld()
    {
        string sharedWorldId = sharedWorldIdInputField.text;
        if (!string.IsNullOrEmpty(sharedWorldId))
        {
            Debug.Log(sharedWorldId + "dunya yukleniyor");
            FirebaseDatabase.DefaultInstance.GetReference($"worldMappings/{sharedWorldId}").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error fetching user ID for shared world: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        string userId = snapshot.Value.ToString();
                        LoadWorld(userId, sharedWorldId);
                    }
                    else
                    {
                        Debug.LogError("Shared world ID not found.");
                    }
                }
            });
        }
        else
        {
            Debug.LogError("Shared world ID is empty.");
        }
    }

}