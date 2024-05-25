using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ModeChangeTool {
    private const string mobileDefine = "UNITY_MOBILE_MODE";

    [MenuItem("Tools/Change to Desktop Mode", false, 1)]
    public static void ChangeToDesktopMode()
    {
        ToggleMode();
    }

    [MenuItem("Tools/Change to Desktop Mode", true)]
    public static bool ChangeToDesktopModeValidate()
    {
        return IsMobileModeEnabled();
    }

    [MenuItem("Tools/Change to Mobile Mode", false, 1)]
    public static void ChangeToMobileMode()
    {
        ToggleMode();
    }

    [MenuItem("Tools/Change to Mobile Mode", true)]
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