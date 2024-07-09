using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using MG_BlocksEngine2.Environment;
using MG_BlocksEngine2.Block.Instruction;
using MG_BlocksEngine2.Block;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class BE2_Cst_ChooseAnObject : BE2_InstructionBase, I_BE2_Instruction {
    private BE2_ProgrammingEnv programmingEnv;
    private SyncScript syncScript;
    private TMP_Dropdown dropdown;
    private Dictionary<string, string> uidToAddressableKeyMap = new Dictionary<string, string>();
    private bool _objectLoadedSuccessfully = false;
    private BE2_TargetObject targetObject;

    protected override void OnAwake()
    {
        base.OnAwake();
    }

    protected override void OnStart()
    {
        var syncScriptObj = GameObject.Find("SyncScript");

        if (syncScriptObj != null) syncScript = syncScriptObj.GetComponent<SyncScript>();

        var programmingEnvObj = GameObject.Find("ProgrammingEnvBE2");

        if (programmingEnvObj != null) programmingEnv = programmingEnvObj.GetComponent<BE2_ProgrammingEnv>();

        dropdown = GameObject.Find("CstDropDown").GetComponent<TMP_Dropdown>();

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            FetchDataFromFirebase();

            if (dropdown.options.Count == 1)
            {
                OnDropdownValueChanged(dropdown.value);
            }
        }

        MakeScriptable.OnScriptableStateChange += FetchDataFromFirebase;
    }

    void OnDestroy()
    {
        MakeScriptable.OnScriptableStateChange -= FetchDataFromFirebase;
    }

    void UpdateDropdown(List<string> options)
    {
        dropdown.ClearOptions();

        if (options.Count > 0)
        {
            dropdown.AddOptions(options);
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            dropdown.interactable = true; // Enable dropdown if there are items
        }
        else
        {
            dropdown.AddOptions(new List<string> { "No items available" });
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            dropdown.interactable = false; // Disable dropdown if there are no items
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (dropdown.options.Count == 0 || index >= dropdown.options.Count)
            return;

        string selectedName = dropdown.options[index].text;
        if (uidToAddressableKeyMap.TryGetValue(selectedName, out string addressableKey))
        {
            LoadAndSetTargetObject(selectedName);
        }
    }

    public void FetchDataFromFirebase()
    {
        List<SynchronousObject> syncObjects = new(FindObjectsOfType<SynchronousObject>());
        List<string> dropdownOptions = new List<string>();

        foreach (var syncObj in syncObjects)
        {
            if (syncObj.syncObject.isScriptable == true)
            {
                dropdownOptions.Add(syncObj.syncObject.name);
                uidToAddressableKeyMap[syncObj.syncObject.name] = syncObj.syncObject.addressableKey;
            }
        }

        UpdateDropdown(dropdownOptions); // Update dropdown regardless of syncObjects count
    }

    SynchronousObject FindSynchronousObjectByName(string name)
    {
        List<SynchronousObject> syncObjects = new(FindObjectsOfType<SynchronousObject>());

        foreach (var syncObj in syncObjects)
        {
            if (syncObj.syncObject.name == name)
            {
                return syncObj;
            }
        }
        return null;
    }

    void LoadAndSetTargetObject(string name)
    {
        SynchronousObject synchronousObject = FindSynchronousObjectByName(name);

        if (synchronousObject != null)
        {
            targetObject = synchronousObject.gameObject.GetComponent<BE2_TargetObject>();

            if (targetObject == null)
            {
                targetObject = synchronousObject.gameObject.AddComponent<BE2_TargetObject>();
            }

            if (uidToAddressableKeyMap.TryGetValue(name, out string addressableKey))
            {
                targetObject.AddressableKey = addressableKey;
            }

            programmingEnv.targetObject = targetObject;
            UpdateTargetObject();
            _objectLoadedSuccessfully = true;
        }

        else
        {
            _objectLoadedSuccessfully = false;
        }
    }

    public new void Function()
    {
        if (_objectLoadedSuccessfully)
        {
            ExecuteNextInstruction();
        }
    }

    public new string Operation()
    {
        return "";
    }
}