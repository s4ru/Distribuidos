using System.Collections;
using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;

public class FriendListController : MonoBehaviour
{
    public static FriendListController Instance { get; private set; } = null;

    [SerializeField] private TMP_InputField tMPEmail, tMPUsername, tMPPassword;
    [HideInInspector] public User currentUser;

    public Dictionary<string, string> usersOnline = new Dictionary<string, string>();
    private DatabaseReference database;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

        }
        else
        {

            Instance = this;
        }
    }

    void Start()
    {
        FirebaseAuth.DefaultInstance.StateChanged += Check_Login;
        database = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void Check_Login(object sender, EventArgs e)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            int scene = SceneManager.GetActiveScene().buildIndex;
            if (scene != 1)
            {
                LoadScene(1);
            }
            else
            {
                currentUser = new User
                {
                    userID = FirebaseAuth.DefaultInstance.CurrentUser.UserId
                };
                GetUser_Username();
            }

        }
    }

    public void Sing_Up()
    {
        string user_Email = tMPEmail.text;
        string user_Password = tMPPassword.text;
        StartCoroutine(RegisterUser(user_Email, user_Password));
    }

    public void Log_In()
    {
        var auth = FirebaseAuth.DefaultInstance;
        auth.SignInWithEmailAndPasswordAsync(tMPEmail.text, tMPPassword.text).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Sign In With Email And Password Async was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Sign In With Email And Password Async encountered an error: " + task.Exception);
                return;
            }
            AuthResult result = task.Result;
        });
    }
    public void GetUser_Username()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users/" + currentUser.userID + "/username")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    currentUser.userUsername = (string)task.Result.Value;
                    FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
                    PopupNotification.Instance.Update_User_Info(currentUser.userUsername);
                    Get_Friends();
                }
            });
    }


    public void Log_Out()
    {
        FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).RemoveValueAsync();
        currentUser.userID = null;
        PlayerPrefs.SetString(nameof(currentUser.userUsername), string.Empty);
        PlayerPrefs.SetString(nameof(currentUser.userID), string.Empty);
        FirebaseAuth.DefaultInstance.SignOut();
        LoadScene(0);
    }
    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
    }

    public void Reset_Password()
    {
        string email = tMPEmail.text;
        StartCoroutine(RestorePassword(email));
    }
    private void Get_Friends()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users/" + currentUser.userID + "/friends")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    foreach (var userFriend in (Dictionary<string, object>)snapshot.Value)
                    {
                        currentUser.userFriends.Add(userFriend.Key, (string)userFriend.Value);
                    }

                    var databaseRef = FirebaseDatabase.DefaultInstance.GetReference("users-online");
                    databaseRef.ChildAdded += HandleChildAdded;
                    databaseRef.ChildRemoved += HandleChildRemoved;

                    var databaseRef2 = FirebaseDatabase.DefaultInstance.GetReference("request");
                    databaseRef2.ChildAdded += HandleChildAdded_Friend;

                    var databaseRef3 = FirebaseDatabase.DefaultInstance.GetReference("accepted");
                    databaseRef3.ChildAdded += HandleChildAdded_Requested_Friend;
                }
            });
    }
    private IEnumerator RestorePassword(string email)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var resetTask = auth.SendPasswordResetEmailAsync(email);

        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.IsCanceled)
        {
            Debug.LogError($"SendPasswordResetEmailAsync is canceled");
        }
        else if (resetTask.IsFaulted)
        {
            Debug.LogError($"SendPasswordResetEmailAsync encountered error" + resetTask.Exception);
        }
        else
        {
            Debug.Log("Password reset email sent successfully to: " + email);
        }
    }
    private IEnumerator RegisterUser(string email, string password)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsCanceled)
        {
            Debug.LogError($"Create User With Email And Passwor dAsync is canceled");
        }
        else if (registerTask.IsFaulted)
        {
            Debug.LogError($"Create User With Email And Password Async encountered error" + registerTask.Exception);
        }
        else
        {
            AuthResult result = registerTask.Result;
            string name = tMPUsername.text;
            User user = new User();
            user.userFriends.Add(result.User.UserId, name);
            database.Child("users").Child(result.User.UserId).Child("username").SetValueAsync(name);
            database.Child("users").Child(result.User.UserId).Child("friends").SetValueAsync(user.userFriends);
        }
    }
    private void OnDestroy()
    {
        FirebaseAuth.DefaultInstance.StateChanged -= Check_Login;
    }

    private void OnApplicationQuit()
    {
        if (currentUser.userID != null)
        {
            FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).RemoveValueAsync();
            currentUser.userID = null;
        }

    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            usersOnline.Add(args.Snapshot.Key, (string)args.Snapshot.Value);
            PopupNotification.Instance.Update_Users(args.Snapshot.Key, (string)args.Snapshot.Value, currentUser.userFriends.ContainsKey(args.Snapshot.Key));
            
        }
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        usersOnline.Remove(args.Snapshot.Key);
        PopupNotification.Instance.Remove_User(args.Snapshot.Key);
    }

    void HandleChildAdded_Friend(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key == currentUser.userID)
        {
            var user = (Dictionary<string, object>)args.Snapshot.Value;
            var userID = user.Keys.ToArray();
            var userUsername = user.Values.ToArray();
            PopupNotification.Instance.Notification_Receved(userID[0], (string)userUsername[0]);
        }
    }

    void HandleChildAdded_Requested_Friend(object sender, ChildChangedEventArgs args)
    {
        Debug.Log(args.Snapshot.Key);
        Debug.Log(currentUser.userID);
        if (args.Snapshot.Key == currentUser.userID)
        {
            var user = (Dictionary<string, object>)args.Snapshot.Value;
            var userID = user.Keys.ToArray();
            var userUsername = user.Values.ToArray();
            Request_Acepted(userID[0], (string)userUsername[0]);
        }
    }

    public void Acept_Request(string ID, string username)
    {
        currentUser.userFriends.Add(ID, username);
        if (usersOnline.ContainsKey(ID)) PopupNotification.Instance.Update_Users(ID, username, true);
        database.Child("users").Child(currentUser.userID).Child("friends").SetValueAsync(currentUser.userFriends);
        database.Child("request").Child(currentUser.userID).RemoveValueAsync();
        database.Child("accepted").Child(ID).Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
    }

    public void Request_Acepted(string ID, string username)
    {
        currentUser.userFriends.Add(ID, username);
        if (usersOnline.ContainsKey(ID)) PopupNotification.Instance.Update_Users(ID, username, true);
        database.Child("users").Child(currentUser.userID).Child("friends").SetValueAsync(currentUser.userFriends);
        database.Child("accepted").Child(currentUser.userID).RemoveValueAsync();
    }

    public void Decline_Request()
    {
        database.Child("request").Child(currentUser.userID).RemoveValueAsync();
    }

    public void SendFriendRequest(string userID)
    {
        database.Child("request").Child(userID).Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
        PopupNotification.Instance.Notification_Send_Activation();
    }
    public void InitMatchmaking()
    {
        database.Child("matchmaking").Child(currentUser.userID).SetValueAsync(currentUser.userUsername);

        LoadScene(2);
    }

}

[System.Serializable]
public class User
{
    public string userID;
    public string userUsername;
    public Dictionary<string, string> userFriends;

    public User()
    {
        userFriends = new Dictionary<string, string>();
    }
}