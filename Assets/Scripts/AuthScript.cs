using Firebase.Auth;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthScript : MonoBehaviour {
    [SerializeField] private TMP_InputField sign_in_email;
    [SerializeField] private TMP_InputField sing_in_password;
    [SerializeField] private TMP_InputField sign_up_email;
    [SerializeField] private TMP_InputField sing_up_password;
    [SerializeField] private TMP_InputField sing_up_password_re;

    [SerializeField] private TextMeshProUGUI signInStatusText;
    [SerializeField] private TextMeshProUGUI signUpStatusText;
    [SerializeField] private TextMeshProUGUI generalStatusText;

    [SerializeField] private GameObject signIn;
    [SerializeField] private GameObject signUp;

    private FirebaseAuth auth;
    public static FirebaseUser user;

    public void Start()
    {
        StartCoroutine(CheckAndFixFirebaseStatus());
    }

    private void InitializeAuth()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private IEnumerator CheckAndFixFirebaseStatus()
    {
        generalStatusText.gameObject.SetActive(true);
        generalStatusText.text = "Checking for auth status...";

        var dependencyTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        Firebase.DependencyStatus dependencyStatus = dependencyTask.Result;

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            InitializeAuth();
            yield return new WaitForEndOfFrame();
            StartCoroutine(CheckForAutoLogin());
        }
        else
        {
            Debug.LogError("Could not resolve all dependencies: " + dependencyStatus.ToString());
        }
    }

    void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    private IEnumerator CheckForAutoLogin()
    {
        if (user != null)
        {
            var reloadUserTask = user.ReloadAsync();
            yield return new WaitUntil(() => reloadUserTask.IsCompleted);
            AutoLogin();
        }
        else
        {
            signIn.SetActive(true);
            generalStatusText.gameObject.SetActive(false);
        }
    }

    private void AutoLogin()
    {
        if (user != null)
        {
            generalStatusText.text = "Loading scene...";
            SceneManager.LoadScene(1);
        }
        else
        {
            signIn.SetActive(true);
            generalStatusText.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    public void EmailPasswordSignUp()
    {
        if (sing_up_password.text != sing_up_password_re.text)
        {
            signUpStatusText.text = "Passwords do not match!";
            return;
        }
        else
        {
            signUpStatusText.text = "";
            signUpStatusText.color = Color.white;
        }
        StartCoroutine(SignUpProcess());
    }

    private IEnumerator SignUpProcess()
    {
        var taskStatus = auth.CreateUserWithEmailAndPasswordAsync(sign_up_email.text, sing_up_password.text);
        yield return new WaitUntil(() => taskStatus.IsCompleted);

        if (taskStatus.IsFaulted)
        {
            Firebase.FirebaseException firebaseEx = taskStatus.Exception?.InnerExceptions[0].InnerException as Firebase.FirebaseException;
            if (firebaseEx != null)
            {
                switch ((AuthError)firebaseEx.ErrorCode)
                {
                    case AuthError.InvalidEmail:
                        SetSignUpErrorStatus("The email address is badly formatted.");
                        break;
                    case AuthError.WeakPassword:
                        SetSignUpErrorStatus("The password is too weak. Please choose a stronger password.");
                        break;
                    default:
                        SetSignUpErrorStatus("An unknown error occurred. Please try again.");
                        break;
                }
            }
            else
            {
                SetSignUpErrorStatus("An unknown error occurred. Please try again.");
            }
            yield break;
        }

        FirebaseUser newUser = taskStatus.Result;
        Debug.LogFormat("User created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
        SendVerificationEmail(newUser);

        SetSignUpStatus("Please verify your email by clicking the link we sent to your email address.");

        yield return new WaitForEndOfFrame();
        SwitchSignInSignUp();
    }

    public void EmailPasswordSignIn()
    {
        StartCoroutine(SignInProcess());
    }

    private IEnumerator SignInProcess()
    {
        var taskStatus = auth.SignInWithEmailAndPasswordAsync(sign_in_email.text, sing_in_password.text);
        yield return new WaitUntil(() => taskStatus.IsCompleted);

        if (taskStatus.IsFaulted)
        {
            Firebase.FirebaseException firebaseEx = taskStatus.Exception?.InnerExceptions[0].InnerException as Firebase.FirebaseException;
            if (firebaseEx != null)
            {
                switch ((AuthError)firebaseEx.ErrorCode)
                {
                    case AuthError.InvalidEmail:
                        SetSignInErrorStatus("The email address is badly formatted.");
                        break;
                    case AuthError.WrongPassword:
                        SetSignInErrorStatus("The password is incorrect. Please try again.");
                        break;
                    case AuthError.UserNotFound:
                        SetSignInErrorStatus("No account found with this email.");
                        break;
                    default:
                        SetSignInErrorStatus("An unknown error occurred. Please try again.");
                        break;
                }
            }
            else
            {
                SetSignInErrorStatus("An unknown error occurred. Please try again.");
            }
            yield break;
        }

        FirebaseUser newUser = taskStatus.Result;
        if (!newUser.IsEmailVerified)
        {
            SetSignInErrorStatus("Please verify your email before logging in.");
        }
        else
        {
            user = newUser;
            SceneManager.LoadScene(1);
        }
    }

    private void SetSignInErrorStatus(string message)
    {
        signInStatusText.color = Color.red;
        signInStatusText.text = message;
    }

    private void SetSignUpErrorStatus(string message)
    {
        signUpStatusText.color = Color.red;
        signUpStatusText.text = message;
    }

    private void SetSignUpStatus(string message)
    {
        signUpStatusText.color = Color.blue;
        signUpStatusText.text = message;
    }

    private void SendVerificationEmail(FirebaseUser user)
    {
        var emailTask = user.SendEmailVerificationAsync();
        emailTask.ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SendEmailVerificationAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SendEmailVerificationAsync encountered an error: " + task.Exception);
                return;
            }
            Debug.Log("Email verification sent successfully.");
        });
    }

    public void SwitchSignInSignUp()
    {
        if (signIn.activeSelf)
        {
            signIn.SetActive(false);
            signUp.SetActive(true);
        }
        else
        {
            signIn.SetActive(true);
            signUp.SetActive(false);
        }
    }
}