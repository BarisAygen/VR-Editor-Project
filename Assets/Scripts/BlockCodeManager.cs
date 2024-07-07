using Firebase.Database;
using MG_BlocksEngine2.Environment;
using MG_BlocksEngine2.Serializer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using UnityEngine.Video;
using System.Collections;

public class BlockCodeManager : MonoBehaviour {
    public Button loadButton;
    private DatabaseReference databaseReference;
    public GameObject blockCodeCanvas;
    private string firebaseKey;

    public static BlockCodeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        loadButton.onClick.AddListener(UploadBlockCodeToFirebase);
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        LoadFirebaseKey();
    }

    public void OnLoadButtonClicked()
    {
        StartCoroutine(LoadBlockCodeFromFirebase());
    }

    public void OnUploadButtonClicked()
    {
        UploadBlockCodeToFirebase();
    }

    public IEnumerator LoadBlockCodeFromFirebase()
    {
        Debug.Log("hi");
        string userId = AuthScript.user.UserId;
        var task = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/worlds/{SyncScript.Instance.currentWorldId}/objects")
            .OrderByChild("addressableKey")
            .EqualTo("blockCode")
            .GetValueAsync();

        yield return new WaitUntil(() => task.IsCompleted); // Wait for Firebase task to complete

        if (task.Exception == null)
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    SyncObject syncObject = JsonUtility.FromJson<SyncObject>(childSnapshot.GetRawJsonValue());
                    if (syncObject.addressableKey == "blockCode")
                    {
                        DeserializeAndLoadBlockCode(syncObject.blockCodeXML, childSnapshot.Key);
                        ExecuteWhenPlayClicked(); // Automatically execute the block code
                        break;
                    }
                }
            }
        }
    }

    private void ExecuteWhenPlayClicked()
    {
        var whenPlayClickedInstruction = FindObjectOfType<BE2_Ins_WhenPlayClicked>();
        if (whenPlayClickedInstruction != null)
        {
            whenPlayClickedInstruction.Function(); // Execute the block code
        }
    }

    public void ToggleBlockCodeVisibility()
    {
        blockCodeCanvas.SetActive(!blockCodeCanvas.activeSelf);
    }

    private void UploadBlockCodeToFirebase()
    {
        string serializedBlockCode = SerializeBlockCode();
        if (string.IsNullOrEmpty(serializedBlockCode))
        {
            return;
        }

        if (string.IsNullOrEmpty(firebaseKey))
        {
            // No key yet, create new data entry
            firebaseKey = databaseReference.Child("users").Child(AuthScript.user.UserId).Child("worlds").Child(SyncScript.Instance.currentWorldId).Child("objects").Push().Key;
            PlayerPrefs.SetString("firebaseKey", firebaseKey); // Save the key locally
            PlayerPrefs.Save();
        }

        SyncObject newObject = new SyncObject
        {
            blockCodeXML = serializedBlockCode,
            isScriptable = false,
            addressableKey = "blockCode",
            uid = firebaseKey,
            type = "BlockCode",
            color = "FFFFFFFF",
            text = "Default BlockCode Text",
            url = "Default URL",
            positions = new List<float>() { 0f, 0f, 0f },
            rotations = new List<float>() { 0f, 0f, 0f, 1f },
            scales = new List<float>() { 1f, 1f, 1f }
        };

        string objectToJson = JsonUtility.ToJson(newObject);
        databaseReference.Child("users").Child(AuthScript.user.UserId).Child("worlds").Child(SyncScript.Instance.currentWorldId).Child("objects").Child(firebaseKey).SetRawJsonValueAsync(objectToJson).ContinueWith(task => { });
    }

    private void LoadFirebaseKey()
    {
        firebaseKey = PlayerPrefs.GetString("firebaseKey", "");
    }

    private void DeserializeAndLoadBlockCode(string serializedBlockCode, string uid)
    {
        I_BE2_ProgrammingEnv programmingEnv = FindProgrammingEnvByUID(uid);
        if (programmingEnv != null)
        {
            BE2_BlocksSerializer.XMLToBlocksCode(serializedBlockCode, programmingEnv);

        }
    }

    private string SerializeBlockCode()
    {
        I_BE2_ProgrammingEnv programmingEnv = FindObjectOfType<BE2_ProgrammingEnv>();
        if (programmingEnv != null)
        {
            return BE2_BlocksSerializer.BlocksCodeToXML(programmingEnv);
        }

        else
        {
            return string.Empty;
        }
    }

    private I_BE2_ProgrammingEnv FindProgrammingEnvByUID(string uid)
    {
        return FindObjectOfType<BE2_ProgrammingEnv>();
    }
}