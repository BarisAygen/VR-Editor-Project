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

        //if (copyWorldIdButton != null)
       // {
        ///    copyWorldIdButton.onClick.AddListener(CopyWorldIdToClipboard);

       // }

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
            yield break;
        }

        var dependencyStatus = dependencyCheck.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            ClearCurrentWorldData(); // Clear existing objects when starting
            FetchAndDisplayWorlds();
        }
    }

    private void ClearCurrentWorldData()
    {
        SyncScript.ClearCurrentWorldData();
    }

    private void FetchAndDisplayWorlds()
    {
        string userId = AuthScript.user.UserId;
        FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds").GetValueAsync().ContinueWith(task => {
            if (task.IsCompleted)
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
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Destroy(buttonObj); // Remove the button from the UI
                        });
                    }
                });
            }
        });
    }

    public void SelectWorld(string worldId)
    {
        LoadWorld(AuthScript.user.UserId, worldId);
    }

    public void CreateNewWorld()
    {
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

                        LoadWorld(userId, newWorldKey);
                    }
                });
            }
        });
    }

    public void LoadWorld(string userId, string worldId)
    {
        FirebaseDatabase.DefaultInstance.GetReference($"users/{userId}/worlds/{worldId}").GetValueAsync().ContinueWith(task => {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        var worldData = JsonUtility.FromJson<WorldData>(snapshot.GetRawJsonValue());
                        InitializeWorld(worldData);
                        SceneManager.LoadScene("Main");
                    });
                }
            }
        });
    }

    public void InitializeWorld(WorldData worldData)
    {
        currentWorldId = worldData.worldId;
        PlayerPrefs.SetString("CurrentWorldId", currentWorldId);
        PlayerPrefs.Save();
    }

    public void LoadSharedWorld()
    {
        string sharedWorldId = sharedWorldIdInputField.text;
        if (!string.IsNullOrEmpty(sharedWorldId))
        {
            FirebaseDatabase.DefaultInstance.GetReference($"worldMappings/{sharedWorldId}").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        string userId = snapshot.Value.ToString();
                        LoadWorld(userId, sharedWorldId);
                    }
                }
            });
        }
    }
}