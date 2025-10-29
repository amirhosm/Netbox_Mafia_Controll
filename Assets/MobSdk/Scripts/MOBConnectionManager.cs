using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

public class MOBConnectionManager : MonoBehaviour
{
    public static MOBConnectionManager Instance { get; private set; }

    [Header("Connection Info")]
    public string tvIP = "";
    public int tvPort = 7777;
    public string myPlayerId = "";
    public bool isConnected = false;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private Thread heartbeatThread;
    private bool isRunning = false;

    // Events
    public event Action OnTVConnected;
    public event Action OnTVFucked;
    public event Action<string> OnTVGotString;
    public event Action<byte[]> OnTVGotNude;
    public event Action<string, string> GotNudeFrom; // playerId, imageData

    private Queue<Action> mainThreadActions = new Queue<Action>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Execute actions on main thread
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    public void ConnectToTV(string ip, int port)
    {
        if (isConnected)
        {
            Debug.LogWarning("Already connected to TV");
            return;
        }

        tvIP = ip;
        tvPort = port;

        Thread connectThread = new Thread(new ThreadStart(ConnectToTVThread));
        connectThread.IsBackground = true;
        connectThread.Start();
    }

    private void ConnectToTVThread()
    {
        try
        {
            client = new TcpClient();
            client.Connect(tvIP, tvPort);
            stream = client.GetStream();
            isConnected = true;
            isRunning = true;

            Debug.Log($"✓ Connected to TV at {tvIP}:{tvPort}");

            // Start receive thread
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Trigger connection event on main thread
            EnqueueMainThreadAction(() =>
            {
                OnTVConnected?.Invoke();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ Failed to connect to TV: {e.Message}");
            isConnected = false;

            EnqueueMainThreadAction(() =>
            {
                OnTVFucked?.Invoke();
            });
        }
    }

    private void ReceiveData()
    {
        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

        while (isRunning && client != null && client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    ProcessMessage(buffer, bytesRead);
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"Error receiving data: {e.Message}");
                    Disconnect();
                }
                break;
            }
        }
    }

    private void ProcessMessage(byte[] data, int length)
    {
        try
        {
            string message = Encoding.UTF8.GetString(data, 0, length);

            if (message.StartsWith("PLAYERID:"))
            {
                myPlayerId = message.Substring(9);
                Debug.Log($"Assigned Player ID: {myPlayerId}");
            }
            else if (message.StartsWith("STRING:"))
            {
                string content = message.Substring(7);
                EnqueueMainThreadAction(() =>
                {
                    OnTVGotString?.Invoke(content);
                });
            }
            else if (message.StartsWith("FROMNUDE:"))
            {
                string content = message.Substring(9);
                EnqueueMainThreadAction(() =>
                {
                    OnTVGotString?.Invoke(content);
                });
            }
            else if (message.StartsWith("IMAGE:"))
            {
                int headerEnd = message.IndexOf('\n');
                int imageSize = int.Parse(message.Substring(6, headerEnd - 6));
                byte[] imageData = new byte[imageSize];
                Array.Copy(data, headerEnd + 1, imageData, 0, imageSize);

                EnqueueMainThreadAction(() =>
                {
                    OnTVGotNude?.Invoke(imageData);
                });
            }
            else if (message.StartsWith("PLAYERNUDE:"))
            {
                // Format: PLAYERNUDE:player-X:imageSize\n[imageData]
                int firstColon = message.IndexOf(':', 11);
                int secondColon = message.IndexOf(':', firstColon + 1);
                int headerEnd = message.IndexOf('\n');

                string senderId = message.Substring(11, firstColon - 11);
                int imageSize = int.Parse(message.Substring(firstColon + 1, headerEnd - firstColon - 1));

                byte[] imageData = new byte[imageSize];
                Array.Copy(data, headerEnd + 1, imageData, 0, imageSize);

                EnqueueMainThreadAction(() =>
                {
                    GotNudeFrom?.Invoke(senderId, Convert.ToBase64String(imageData));
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing message: {e.Message}");
        }
    }

    // Public API Functions

    // Send string to TV
    public void SendStringToTV(string message)
    {
        SendData($"STRING:{message}");
    }

    // Send image to TV
    public void SendImageToTV(byte[] imageData)
    {
        byte[] header = Encoding.UTF8.GetBytes($"IMAGE:{imageData.Length}\n");
        byte[] fullMessage = new byte[header.Length + imageData.Length];
        Array.Copy(header, 0, fullMessage, 0, header.Length);
        Array.Copy(imageData, 0, fullMessage, header.Length, imageData.Length);

        SendData(fullMessage);
    }

    // Send texture to TV
    public void SendTextureToTV(Texture2D texture)
    {
        byte[] imageData = texture.EncodeToPNG();
        SendImageToTV(imageData);
    }

    // Send button input to TV
    public void SendInput(string buttonData)
    {
        SendData($"INPUT:{buttonData}");
    }

    // Send gyro/accelerometer data to TV
    public void SendGyroData(Vector3 gyro, Vector3 accel)
    {
        string gyroData = $"G:{gyro.x:F2},{gyro.y:F2},{gyro.z:F2}|A:{accel.x:F2},{accel.y:F2},{accel.z:F2}";
        SendData($"GYRO:{gyroData}");
    }

    // Send air mouse position
    public void SendMousePosition(Vector2 position)
    {
        SendData($"MOUSE:{position.x:F0},{position.y:F0}");
    }

    // Get list of all connected devices from TV (request)
    public void GetAllThoseBitches()
    {
        SendData("GETDEVICES");
    }

    // Send string/image to another mobile device by player ID
    public void SendMyNudeTo(string targetPlayerId, string message)
    {
        SendData($"TONUDE:{targetPlayerId}:{message}");
    }

    public void SendMyMessageTo(string targetPlayerId, string message)
    {
        SendData($"FORWARD:{targetPlayerId}:{message}");
    }

    public void SendImageToPlayer(string targetPlayerId, byte[] imageData)
    {
        byte[] header = Encoding.UTF8.GetBytes($"TOIMGNUDE:{targetPlayerId}:{imageData.Length}\n");
        byte[] fullMessage = new byte[header.Length + imageData.Length];
        Array.Copy(header, 0, fullMessage, 0, header.Length);
        Array.Copy(imageData, 0, fullMessage, header.Length, imageData.Length);

        SendData(fullMessage);
    }

    private void SendData(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        SendData(data);
    }

    private void SendData(byte[] data)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogWarning("Not connected to TV");
            return;
        }

        try
        {
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending data: {e.Message}");
            Disconnect();
        }
    }

    public void Disconnect()
    {
        isRunning = false;
        isConnected = false;

        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        EnqueueMainThreadAction(() =>
        {
            OnTVFucked?.Invoke();
        });

        Debug.Log("Disconnected from TV");
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            // App going to background
            Debug.Log("App paused");
        }
    }
}