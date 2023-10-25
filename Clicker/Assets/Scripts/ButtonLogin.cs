using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogin : MonoBehaviour
{
    [SerializeField] 
    private Button _loginButton;
    [SerializeField] 
    private TMP_InputField _emailInputField;
    [SerializeField] 
    private TMP_InputField _emailPasswordField;
    private DatabaseReference mDatabaseRef;
    void Reset() 
    { _loginButton = GetComponent<Button>();
        _emailInputField = GameObject.Find("InputEmail").GetComponent<TMP_InputField>(); 
        _emailPasswordField = GameObject.Find("InputPassword").GetComponent<TMP_InputField>(); 
    }
    void Start() 
    { 
        _loginButton.onClick.AddListener(HandleLoginButtonClicked);
        mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
    }
    private void HandleLoginButtonClicked() 
    { 
        var auth = FirebaseAuth.DefaultInstance; 
        auth.SignInWithEmailAndPasswordAsync(_emailInputField.text, _emailPasswordField.text).ContinueWith(task => 
        { 
            if (task.IsCanceled) 
            { 
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled."); 
                return; 
            } 
            if (task.IsFaulted) 
            { 
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception); 
                return; 
            } 
            Firebase.Auth.AuthResult result = task.Result;           
            Debug.LogFormat("User signed in successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);
        }); 
    }    
}
