using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class SceneScript : NetworkBehaviour
{
    public Text canvasStatusText;
    public PlayerScript playerScript;

    [SyncVar(hook = nameof(OnStatusTextChanged))]
    public string statusText;

    void OnStatusTextChanged(string _old, string _new)
    {
        //called from syhnc var hook, to update info on screen for all players.
        canvasStatusText.text = statusText;
    }

    public void ButtonSendMessage()
    {
        if (playerScript != null)
        {
            playerScript.CmdSendPlayerMessage();
        }
    }

    
}
