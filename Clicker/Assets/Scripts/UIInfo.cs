using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIInfo : MonoBehaviour
{
    public Image image;

    public TextMeshProUGUI txt;

    public Button button;

    public int index;

    public bool inUse;

    public bool friend;

    public string id;

    void Start()
    {
        button.onClick.AddListener(SendRequest);
    }

    public void SendRequest()
    {
        FriendListController.Instance.SendFriendRequest(id);
    }
}
