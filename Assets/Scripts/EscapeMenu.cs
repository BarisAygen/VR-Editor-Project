using Firebase.Auth;
using Google.XR.Cardboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeMenu : MonoBehaviour
{
    [SerializeField] private GameObject escapeMenu;

    // Update is called once per frame
    void Update()
    {
        #if !UNITY_MOBILE_MODE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenEscapeMenu();
        }
        #else
        if (Api.IsGearButtonPressed)
        {
            OpenEscapeMenu();
        }
        #endif

    }

    public void OpenEscapeMenu()
    {
        escapeMenu.SetActive(!escapeMenu.activeSelf);
    }

    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        SceneManager.LoadScene(0);
    }

    public void Exit()
    {
#if !UNITY_MOBILE_MODE && UNITY_EDITOR
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        Application.Quit();
    }

}
