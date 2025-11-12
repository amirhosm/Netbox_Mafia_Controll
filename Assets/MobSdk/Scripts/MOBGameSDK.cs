using UnityEngine;
using System.Collections.Generic;
using System;

// Inherit from this in your mobile game scripts
public class MOBGameSDK : MonoBehaviour
{
    GameManager gameManager;
    protected MOBConnectionManager connectionManager;

    protected virtual void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;

        connectionManager = MOBConnectionManager.Instance;
        gameManager = GetComponent<GameManager>();

        // Subscribe to events
        connectionManager.OnTVConnected += OnTVConnected;
        connectionManager.OnTVFucked += OnTVFucked;
        connectionManager.OnTVGotString += OnTVGotString;
        //connectionManager.OnTVGotNude += OnTVGotNude;
        connectionManager.GotNudeFrom += GotNudeFrom;
        connectionManager.OnGotAvatar += GotAvatar;
    }

   

    // Override these in your game
    protected virtual void OnTVConnected()
    {
        Debug.Log("[Game] Connected to TV");
        // Your game logic here
    }

    protected virtual void OnTVFucked()
    {
        Debug.Log("[Game] Disconnected from TV");
        gameManager.ResetAll();
    }

    protected virtual void OnTVGotString(string message)
    {
        Debug.Log($"[Game] Got string from TV: {message}");
        if (message.StartsWith("LobbyStart"))
        {
            gameManager.GotoRolesPanel();
        }
        else if (message.StartsWith("Role"))
        {
            gameManager.GotoShowRolePanel(message.Split(':')[1], message.Split(':')[2], message.Split(':')[3]);
        }
        else if (message.StartsWith("DayTalk"))
        {
            gameManager.ShowDayTalk(message.Split(':')[1], message.Split(':')[2]);
        }
        else if (message.StartsWith("DayVote"))
        {
            gameManager.ShowDayVote(message.Split(':')[1], message.Split(':')[2]);
        }
        else if (message.StartsWith("EndVoting"))
        {
            gameManager.EndDayVoting();
        }
        else if (message.StartsWith("Die"))
        {
            gameManager.ShowDie();
        }
        else if (message.StartsWith("Win_mafia"))
        {
            gameManager.ShowWinMafia();
        }
        else if (message.StartsWith("Win_citizen"))
        {
            gameManager.ShowWinCitizen();
        }
        else if (message.StartsWith("Kicked"))
        {
            gameManager.ShowKicked();
        }
        else if (message.StartsWith("NightPlayers"))
        {
            string[] Datas = message.Split("|");
            gameManager.GotoNight(Datas);
        }
        else if (message.StartsWith("EndNight"))
        {
            gameManager.EndNight();
        }
        else if (message.StartsWith("NightMafiaKill"))
        {
            gameManager.MafiaToGodfatherInNight(message.Split(':')[1]);
        }
    }

    private void GotAvatar(string arg1, byte[] arg2, string playerId)
    {
        gameManager.AddAvatar(playerId, arg2);
    }

    protected virtual void OnTVGotNude(byte[] imageData)
    {
        Debug.Log($"[Game] Got image from TV, size: {imageData.Length} bytes");
        // Convert to texture if needed:
        // Texture2D texture = new Texture2D(2, 2);
        // texture.LoadImage(imageData);
    }

    protected virtual void GotNudeFrom(string playerId, string message)
    {
        Debug.Log($"[Game] Got image from player {playerId}");
        // Convert base64 to texture if needed:
        // byte[] imageData = System.Convert.FromBase64String(imageDataBase64);
        // Texture2D texture = new Texture2D(2, 2);
        // texture.LoadImage(imageData);
        
    }

    // Helper functions for your game

    // Send string to TV
    public void SendStringToTV(string message)
    {
        connectionManager.SendStringToTV(message);
    }

    // Send image to TV
    protected void SendImageToTV(byte[] imageData)
    {
        connectionManager.SendImageToTV(imageData);
    }

    // Send texture to TV
    protected void SendTextureToTV(Texture2D texture)
    {
        connectionManager.SendTextureToTV(texture);
    }

    // Get list of all connected devices from TV
    protected void GetAllThoseBitches()
    {
        connectionManager.GetAllThoseBitches();
    }

    // Send message to another mobile player
    public void SendMyMessageTo(string targetPlayerId, string message)
    {
        connectionManager.SendMyMessageTo(targetPlayerId, message);
    }
    public void SendMyNudeTo(string targetPlayerId, string message)
    {
        connectionManager.SendMyNudeTo(targetPlayerId, message);
    }

    // Send image to another mobile player
    protected void SendImageToPlayer(string targetPlayerId, byte[] imageData)
    {
        connectionManager.SendImageToPlayer(targetPlayerId, imageData);
    }

    // Send texture to another mobile player
    protected void SendTextureToPlayer(string targetPlayerId, Texture2D texture)
    {
        byte[] imageData = texture.EncodeToPNG();
        connectionManager.SendImageToPlayer(targetPlayerId, imageData);
    }

    // Check if connected
    protected bool IsConnectedToTV()
    {
        return connectionManager.isConnected;
    }

    // Get my player ID
    public string GetMyPlayerId()
    {
        return connectionManager.myPlayerId;
    }

    // Take screenshot and send to TV
    protected void SendScreenshotToTV()
    {
        StartCoroutine(CaptureAndSendScreenshot());
    }

    private System.Collections.IEnumerator CaptureAndSendScreenshot()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        SendTextureToTV(screenshot);
        Destroy(screenshot);
    }

    protected virtual void OnDestroy()
    {
        if (connectionManager != null)
        {
            connectionManager.OnTVConnected -= OnTVConnected;
            connectionManager.OnTVFucked -= OnTVFucked;
            connectionManager.OnTVGotString -= OnTVGotString;
            //connectionManager.OnTVGotNude -= OnTVGotNude;
            connectionManager.GotNudeFrom -= GotNudeFrom;
        }
    }
}

// Example usage in your mobile game:
/*
public class MyMobileGame : MOBGameSDK
{
    protected override void OnTVConnected()
    {
        base.OnTVConnected();
        
        // Send ready message to TV
        SendStringToTV("Player is ready!");
    }

    protected override void OnTVGotString(string message)
    {
        base.OnTVGotString(message);
        
        if (message == "START_GAME")
        {
            // Start your game
        }
    }

    private void Update()
    {
        if (IsConnectedToTV())
        {
            // Your game loop
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SendStringToTV("JUMP");
            }
        }
    }
}
*/