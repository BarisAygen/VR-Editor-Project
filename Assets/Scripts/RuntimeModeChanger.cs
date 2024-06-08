#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RuntimeModeChange : MonoBehaviour {
    private const string mobileDefine = "UNITY_MOBILE_MODE";

    public static bool ChangeToMobileModeValidate()
    {
        return !IsMobileModeEnabled();
    }

    public static void ToggleMode()
    {
        BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup);
        var definesList = new List<string>(currentDefines.Split(';'));

        if (definesList.Contains(mobileDefine))
        {
            definesList.Remove(mobileDefine);
        }
        else
        {
            definesList.Add(mobileDefine);
        }

        definesList.RemoveAll(string.IsNullOrEmpty);
        currentDefines = string.Join(";", definesList);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup, currentDefines);
        Debug.Log($"Switched to {(definesList.Contains(mobileDefine) ? "Mobile" : "Desktop")} mode.");
    }

    private static bool IsMobileModeEnabled()
    {
        BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup);
        return currentDefines.Contains(mobileDefine);
    }
}
#endif