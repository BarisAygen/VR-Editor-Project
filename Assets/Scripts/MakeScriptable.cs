using Firebase.Database;
using System;
using System.Collections;
using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MakeScriptable : MonoBehaviour {
    private Transform mainObject; // Object to add action
    [SerializeField] private Button closeButton; // Close block code UI
    [SerializeField] private Toggle scriptableToggle;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_InputField objectNameInputField;
    [SerializeField] private TMP_InputField objectSpeechInputField;
    [SerializeField] private RectTransform makeScriptableCanvasParent;
    [SerializeField] private GameObject makeScriptableCanvas; // Spawn when a object is clicked
    [SerializeField] private Button attachButton;
    [SerializeField] private Text attachButtonText;
    [SerializeField] private TextMeshProUGUI attachmentModeText; // Use Text if you're not using TextMeshPro
    [SerializeField] private GameObject speechBubblePrefab;
    [SerializeField] private GameObject thinkBubblePrefab;
    [SerializeField] private TMP_InputField speechInputField;
    [SerializeField] private Button speakButton;
    [SerializeField] private Button thinkButton;
    [SerializeField] private GameObject inputPanel;
    public SyncScript syncScript;
    private bool canvasClickedThisFrame = false; // To check raycasting penetration issue
    public static bool isInputFieldFocused = false;
    private GameObject currentBubble; // Keep track of the current speech or thinking bubble
    public delegate void ScriptableStateChangeAction();
    public static event ScriptableStateChangeAction OnScriptableStateChange;
    public static bool isAttachmentMode = false;
    private Transform attachFirstObject;
    private Dictionary<Transform, GameObject> bubbles = new Dictionary<Transform, GameObject>();

    private void Start()
    {
        #region Mobile
#if !UNITY_MOBILE_MODE
        scriptableToggle.onValueChanged.AddListener(OnMakeScriptableObjectButtonClick);
#endif
        #endregion
        confirmButton.onClick.AddListener(ConfirmNameChange);
        attachButton.onClick.AddListener(StartAttachmentMode);
        objectNameInputField.onSelect.AddListener(delegate { isInputFieldFocused = true; });
        objectNameInputField.onDeselect.AddListener(delegate { isInputFieldFocused = false; });
        objectSpeechInputField.onSelect.AddListener(delegate { isInputFieldFocused = true; });
        objectSpeechInputField.onDeselect.AddListener(delegate { isInputFieldFocused = false; });
        speakButton.onClick.AddListener(() => {
            if (mainObject != null)
            {
                CreateSpeechBubble(speechInputField.text, false, mainObject.transform);
            }
        });
        thinkButton.onClick.AddListener(() => {
            if (mainObject != null)
            {
                CreateSpeechBubble(speechInputField.text, true, mainObject.transform);
            }
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isAttachmentMode)
        {
            ExitAttachmentMode();
        }

        if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int layerMask = 1 << LayerMask.NameToLayer("UI");
            layerMask = ~layerMask;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                if (isAttachmentMode)
                {
                    if (hit.transform != attachFirstObject)
                    {
                        AttachObjects(attachFirstObject, hit.transform);
                        isAttachmentMode = false;
                    }
                }
                else
                {
                    if (!canvasClickedThisFrame && hit.transform.gameObject != makeScriptableCanvas)
                    {
                        AssignMainObjectAndCreateButton(hit.transform);
                        UpdateScriptableToggle(hit.transform);
                    }
                }
            }

            canvasClickedThisFrame = false;
        }

        if (mainObject != null && makeScriptableCanvas.activeSelf)
        {
            UpdateCanvasPosition();
        }

        if (inputPanel.activeSelf)
        {
            UpdateInputPanelPosition();
        }

        if (mainObject == null || !mainObject.gameObject.activeInHierarchy)
        {
            if (makeScriptableCanvas.activeSelf)
            {
                makeScriptableCanvas.SetActive(false);
            }
            return;
        }

        // Update bubble positions
        foreach (var entry in bubbles)
        {
            if (entry.Key != null)
            {
                entry.Value.transform.position = entry.Key.position + new Vector3(0, 3.0f, 0);
            }
        }
    }

    private void UpdateInputPanelPosition()
    {
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(mainObject.position);
        inputPanel.transform.position = screenPosition + new Vector3(0, 150f);  // Adjust the offset as needed
    }

    public void CanvasClicked() // Stop raycast
    {
        canvasClickedThisFrame = true;
    }

    private void AssignMainObjectAndCreateButton(Transform objectTransform)
    {
        mainObject = objectTransform;
        UpdateAttachButton(mainObject);

        if (mainObject != null)
        {
            objectNameInputField.text = mainObject.name;
        }

        CreateScreenButtonAtObject();
    }

    private void UpdateCanvasPosition()
    {
        if (mainObject != null)
        {
            Vector2 anchoredPos;
            Vector2 offset = new Vector2(0, 150f);
            Vector3 objectPosition = mainObject.position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(objectPosition);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                makeScriptableCanvasParent, screenPosition, null, out anchoredPos))
            {
                makeScriptableCanvas.GetComponent<RectTransform>().anchoredPosition = anchoredPos + offset;
            }
        }
    }

    private void CreateScreenButtonAtObject()
    {
        if (mainObject != null)
        {
            makeScriptableCanvas.SetActive(true);
            Vector2 anchoredPos;
            Vector2 offset = new Vector2(0, 150f);
            Vector3 objectPosition = mainObject.position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(objectPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(makeScriptableCanvasParent, screenPosition, null, out anchoredPos);
            makeScriptableCanvas.GetComponent<RectTransform>().anchoredPosition = anchoredPos + offset;
            UpdateScriptableToggle(mainObject);
        }
    }

    #region Mobile
#if !UNITY_MOBILE_MODE
    public void OnMakeScriptableObjectButtonClick(bool isScriptable)
    {
        if (mainObject != null)
        {
            syncScript.MakeObjectScriptable(mainObject, isScriptable);
            UpdateScriptableToggle(mainObject); // Update the state of the toggle based on the new scriptability of the object
            OnScriptableStateChange?.Invoke();
        }
    }
#endif
    #endregion

    private void UpdateScriptableToggle(Transform objectTransform)
    {
        SynchronousObject syncObjectComponent = objectTransform.GetComponent<SynchronousObject>();
        if (syncObjectComponent != null)
        {
            scriptableToggle.isOn = syncObjectComponent.syncObject.isScriptable;
        }
        else
        {
            scriptableToggle.isOn = false;
        }
    }

    public void ScriptableCanvasDisableButton()
    {
        makeScriptableCanvas.gameObject.SetActive(false);
        inputPanel.gameObject.SetActive(false);
    }
    public void ConfirmNameChange()
    {
        if (mainObject != null && objectNameInputField != null)
        {
            mainObject.name = objectNameInputField.text;
            SynchronousObject syncObjectComponent = mainObject.GetComponent<SynchronousObject>();
            if (syncObjectComponent != null)
            {
                SyncObject syncObject = syncObjectComponent.syncObject;
                syncObject.name = objectNameInputField.text;
                syncObject.nameChanged = true;
                UpdateNameInFirebase(syncObject);
            }
        }
    }

    private void UpdateNameInFirebase(SyncObject syncObject)
    {
        string objectToJson = JsonUtility.ToJson(syncObject);
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).Child("worlds").Child(SyncScript.Instance.currentWorldId).Child("objects");
        reference.Child(syncObject.uid).SetRawJsonValueAsync(objectToJson).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Name updated in Firebase.");
            }
        });
    }

    private void UpdateAttachButton(Transform objectTransform)
    {
        if (objectTransform.parent != null)
        {
            attachButtonText.text = "Unattach";
            attachButton.onClick.RemoveAllListeners();
            attachButton.onClick.AddListener(StartUnattachmentMode);
        }

        else
        {
            attachButtonText.text = "Attach";
            attachButton.onClick.RemoveAllListeners();
            attachButton.onClick.AddListener(StartAttachmentMode);
        }
    }
    private void StartAttachmentMode()
    {
        if (mainObject != null)
        {
            isAttachmentMode = true;
            attachFirstObject = mainObject;
            makeScriptableCanvas.SetActive(false);
            attachmentModeText.gameObject.SetActive(true);
        }
    }

    private void AttachObjects(Transform first, Transform second)
    {
        // Determine bounding boxes to calculate new position
        Bounds firstBounds = first.GetComponent<Renderer>().bounds;
        Bounds secondBounds = second.GetComponent<Renderer>().bounds;

        // Adjust position so that 'first' (mainObject) is correctly positioned relative to 'second'
        Vector3 newPosition = second.position;
        newPosition.x += secondBounds.extents.x + firstBounds.extents.x; // Adjust positioning logic if necessary
        first.position = newPosition;

        // Set 'first' as the child of 'second'
        first.SetParent(second);

        // Update the SyncObject components for Firebase
        SynchronousObject firstSync = first.GetComponent<SynchronousObject>();
        SynchronousObject secondSync = second.GetComponent<SynchronousObject>();
        if (firstSync != null && secondSync != null)
        {
            firstSync.syncObject.parentUid = secondSync.syncObject.uid; // Update parent UID
            string objectToJson = JsonUtility.ToJson(firstSync.syncObject);
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).Child("worlds").Child(SyncScript.Instance.currentWorldId).Child("objects");
            reference.Child(firstSync.syncObject.uid).SetRawJsonValueAsync(objectToJson).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Child updated in Firebase.");
                }
            });
        }

        // Exit attachment mode
        ExitAttachmentMode();
    }

    private void ExitAttachmentMode()
    {
        isAttachmentMode = false;
        makeScriptableCanvas.SetActive(false);
        attachmentModeText.gameObject.SetActive(false);
    }

    private void StartUnattachmentMode()
    {
        if (mainObject != null)
        {
            // Detach the object
            mainObject.SetParent(null);

            // Update Firebase
            UpdateParentInFirebase(mainObject, null);

            // Update UI
            attachButtonText.text = "Attach";
            attachButton.onClick.RemoveAllListeners();
            attachButton.onClick.AddListener(StartAttachmentMode);
        }
    }

    public void DeleteParentObject(Transform parentTransform)
    {
        // Check if the parent has any children
        if (parentTransform.childCount > 0)
        {
            Transform firstChild = parentTransform.GetChild(0);
            // Unattach the first child
            firstChild.SetParent(null);

            // Optionally update Firebase to remove the parent link
            UpdateParentInFirebase(firstChild, "");

            // Update Firebase that the parent no longer has this child
            SynchronousObject parentSyncObject = parentTransform.GetComponent<SynchronousObject>();
            if (parentSyncObject != null)
            {
                // Assuming the children are listed or linked in Firebase
                RemoveChildFromFirebaseParent(parentSyncObject.syncObject.uid, firstChild.GetComponent<SynchronousObject>().syncObject.uid);
            }
        }

        // Now delete the parent object from Unity
        SynchronousObject syncObject = parentTransform.GetComponent<SynchronousObject>();
        if (syncObject != null)
        {
            DeleteFromFirebase(syncObject.syncObject.uid);
        }

        // Destroy the parent GameObject
        Destroy(parentTransform.gameObject);
    }

    private void UpdateParentInFirebase(Transform child, string newParentUid)
    {
        SynchronousObject syncObject = child.GetComponent<SynchronousObject>();
        if (syncObject != null)
        {
            syncObject.syncObject.parentUid = newParentUid; // "" for no parent

            string json = JsonUtility.ToJson(syncObject.syncObject);
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId);
            reference.Child(syncObject.syncObject.uid).SetRawJsonValueAsync(json).ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("Parent updated successfully in Firebase for " + child.name);
                }
                else
                {
                    Debug.LogError("Failed to update parent in Firebase: " + task.Exception.ToString());
                }
            });
        }
    }

    private void DeleteFromFirebase(string uid)
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).Child(uid);
        reference.RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("Successfully deleted " + uid + " from Firebase");
            }
            else
            {
                Debug.LogError("Error deleting " + uid + " from Firebase: " + task.Exception);
            }
        });
    }

    private void RemoveChildFromFirebaseParent(string parentUid, string childUid)
    {
        // Get the reference to the parent object in Firebase
        DatabaseReference parentRef = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId).Child(parentUid);

        // Fetch the current parent object from Firebase
        parentRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Handle the error...
                Debug.LogError("Error fetching parent data: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.HasChildren)
                {
                    List<string> childUids = snapshot.Child("childUids").Value as List<string>;
                    if (childUids != null && childUids.Contains(childUid))
                    {
                        // Remove the child UID from the list
                        childUids.Remove(childUid);

                        // Update the childUids in Firebase
                        parentRef.Child("childUids").SetValueAsync(childUids).ContinueWith(updateTask =>
                        {
                            if (updateTask.IsFaulted)
                            {
                                Debug.LogError("Failed to update children in Firebase: " + updateTask.Exception);
                            }
                            else
                            {
                                Debug.Log("Child removed from parent in Firebase successfully.");
                            }
                        });
                    }
                }
            }
        });
    }

    public void OnSpeakOrTalkButtonClick()
    {
        if (mainObject != null)
        {
            makeScriptableCanvas.SetActive(false);
            inputPanel.SetActive(true);
        }
    }

    public IEnumerator InitializeBubblesFromFirebase()
    {
        yield return new WaitForSeconds(1); // Wait for objects to load properly
        foreach (var syncObject in FindObjectsOfType<SynchronousObject>())
        {
            if (syncObject.syncObject.isSpeaking)
            {
                CreateSpeechBubble(syncObject.syncObject.speakingText, false, syncObject.transform);
            }
            else if (syncObject.syncObject.isThinking)
            {
                CreateSpeechBubble(syncObject.syncObject.thinkingText, true, syncObject.transform);
            }
        }
    }

    public void CreateSpeechBubble(string text, bool isThinking, Transform targetObject)
    {
        if (targetObject == null) return;

        GameObject bubblePrefab = isThinking ? thinkBubblePrefab : speechBubblePrefab;
        SynchronousObject syncObjectComponent = targetObject.GetComponent<SynchronousObject>();

        // Check if the object already has a bubble
        if (bubbles.ContainsKey(targetObject))
        {
            Destroy(bubbles[targetObject]);
            bubbles.Remove(targetObject);
        }

        if (!string.IsNullOrEmpty(text))
        {
            GameObject bubble = Instantiate(bubblePrefab, targetObject);
            bubble.transform.localPosition = new Vector3(0, 3.0f, 0); // Position relative to the parent
            TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();

            if (textComponent != null)
            {
                textComponent.text = text;
            }

            // Add the new bubble to the dictionary
            bubbles[targetObject] = bubble;

            // Update Firebase
            if (isThinking)
            {
                syncObjectComponent.syncObject.isThinking = true;
                syncObjectComponent.syncObject.thinkingText = text;
            }
            else
            {
                syncObjectComponent.syncObject.isSpeaking = true;
                syncObjectComponent.syncObject.speakingText = text;
            }

            UpdateTextInFirebase(syncObjectComponent.syncObject);
        }
        else
        {
            // Update Firebase
            if (isThinking)
            {
                syncObjectComponent.syncObject.isThinking = false;
                syncObjectComponent.syncObject.thinkingText = "";
            }
            else
            {
                syncObjectComponent.syncObject.isSpeaking = false;
                syncObjectComponent.syncObject.speakingText = "";
            }

            UpdateTextInFirebase(syncObjectComponent.syncObject);
        }
    }

    private void UpdateTextInFirebase(SyncObject syncObject)
    {
        string objectToJson = JsonUtility.ToJson(syncObject);
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(AuthScript.user.UserId);
        reference.Child(syncObject.uid).SetRawJsonValueAsync(objectToJson).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Text updated in Firebase.");
            }
        });
    }
}