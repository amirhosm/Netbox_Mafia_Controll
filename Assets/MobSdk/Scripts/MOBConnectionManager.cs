using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if !UNITY_WEBGL || UNITY_EDITOR
using WebSocketSharp;
#endif

public class MOBConnectionManager : MonoBehaviour
{
    public static MOBConnectionManager Instance { get; private set; }

    [Header("Connection Settings")]
    public string serverIP = "192.168.1.100";
    public int tcpPort = 7777; // For mobile native builds
    public int wsPort = 7778; // For WebGL
    public string wsPath = "/mobile";

    private TcpClient tcpClient;
    private NetworkStream tcpStream;

#if !UNITY_WEBGL || UNITY_EDITOR
    private WebSocket wsClient;
#else
    private int webSocketId = -1;
    private Queue<byte[]> messageQueue = new Queue<byte[]>();
#endif

    private Thread receiveThread;
    public bool isConnected = false;
    public string myPlayerId = "";
    private bool isRunning = false;
    private bool isWebGL = false;

    public event Action OnTVConnected;
    public event Action OnTVFucked;
    public event Action<string> OnTVGotString;
    public event Action<string, byte[]> OnTVGotNude;
    public event Action<string, byte[], string> OnGotAvatar;
    public event Action<string, string> GotNudeFrom;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int WebSocketConnect(string url);
    
    [DllImport("__Internal")]
    private static extern int WebSocketGetState(int socketId);
    
    [DllImport("__Internal")]
    private static extern void WebSocketSend(int socketId, byte[] data, int length);
    
    [DllImport("__Internal")]
    private static extern void WebSocketSendText(int socketId, string message);
    
    [DllImport("__Internal")]
    private static extern void WebSocketClose(int socketId);
    
    [DllImport("__Internal")]
    private static extern int WebSocketReceive(int socketId, byte[] buffer, int bufferSize);
#endif

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
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        isWebGL = true;
        Debug.Log("Running in WebGL mode");
#else
        isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
        Debug.Log($"Running in {Application.platform} mode");
#endif
    }

    public void ConnectToTV(string ip, int port)
    {
        serverIP = ip;

        Debug.Log($"ConnectToTV called with IP: {ip}, Port: {port}, IsWebGL: {isWebGL}");

        if (isWebGL)
        {
            wsPort = port;
            ConnectWebSocket();
        }
        else
        {
            tcpPort = port;
            ConnectTcp();
        }
    }

    public void ConnectToTV()
    {
        ConnectToTV(serverIP, isWebGL ? wsPort : tcpPort);
    }

    private void ConnectTcp()
    {
        try
        {
            Debug.Log($"Attempting TCP connection to {serverIP}:{tcpPort}");
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIP, tcpPort);
            tcpStream = tcpClient.GetStream();
            isConnected = true;
            isRunning = true;

            receiveThread = new Thread(ReceiveTcpMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"✓ TCP Connected to TV at {serverIP}:{tcpPort}");

            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnTVConnected?.Invoke());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ TCP Connect failed: {e.Message}\n{e.StackTrace}");
            isConnected = false;

            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnTVFucked?.Invoke());
            }
        }
    }

    private void ConnectWebSocket()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string url = $"ws://{serverIP}:{wsPort}{wsPath}";
            Debug.Log($"Attempting WebGL WebSocket connection to: {url}");
            
            webSocketId = WebSocketConnect(url);
            
            if (webSocketId >= 0)
            {
                isRunning = true;
                Debug.Log($"✓ WebSocket connection initiated with ID: {webSocketId}");
            }
            else
            {
                Debug.LogError($"✗ WebSocket Connect failed: Invalid socket ID");
                isConnected = false;
                OnTVFucked?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ WebSocket Connect failed: {e.Message}\n{e.StackTrace}");
            isConnected = false;
            OnTVFucked?.Invoke();
        }
#else
        try
        {
            string url = $"ws://{serverIP}:{wsPort}{wsPath}";
            Debug.Log($"Attempting Editor WebSocket connection to: {url}");

            wsClient = new WebSocket(url);

            wsClient.OnOpen += (sender, e) =>
            {
                isConnected = true;
                isRunning = true;
                Debug.Log($"✓ WebSocket Connected to TV at {url}");

                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => OnTVConnected?.Invoke());
                }
            };

            wsClient.OnMessage += (sender, e) =>
            {
                string message = e.IsText ? e.Data : Encoding.UTF8.GetString(e.RawData);
                Debug.Log($"WebSocket message received: {message.Substring(0, Math.Min(100, message.Length))}...");

                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => ProcessMessage(message));
                }
            };

            wsClient.OnError += (sender, e) =>
            {
                Debug.LogError($"✗ WebSocket error: {e.Message}");

                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => Disconnect());
                }
            };

            wsClient.OnClose += (sender, e) =>
            {
                Debug.Log($"WebSocket closed: Code={e.Code}, Reason={e.Reason}");

                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => Disconnect());
                }
            };

            wsClient.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ WebSocket Connect failed: {e.Message}\n{e.StackTrace}");
            isConnected = false;
            OnTVFucked?.Invoke();
        }
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void Update()
    {
        if (isWebGL && isRunning && webSocketId >= 0)
        {
            int state = WebSocketGetState(webSocketId);
            
            // State: 0=CONNECTING, 1=OPEN, 2=CLOSING, 3=CLOSED
            if (state == 1 && !isConnected)
            {
                isConnected = true;
                Debug.Log("✓ WebGL WebSocket opened");
                OnTVConnected?.Invoke();
            }
            else if (state == 3 && isConnected)
            {
                Debug.LogWarning("WebSocket closed by server");
                Disconnect();
            }
            
            // Receive messages
            if (isConnected)
            {
                byte[] buffer = new byte[1024 * 1024];
                int bytesRead = WebSocketReceive(webSocketId, buffer, buffer.Length);
                
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"WebGL received: {message.Substring(0, Math.Min(100, message.Length))}...");
                    ProcessMessage(message);
                }
            }
        }
    }
#endif

    private void ReceiveTcpMessages()
    {
        try
        {
            byte[] buffer = new byte[1024 * 1024];
            while (isRunning && tcpClient != null && tcpClient.Connected)
            {
                int bytesRead = tcpStream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (UnityMainThreadDispatcher.Instance != null)
                    {
                        UnityMainThreadDispatcher.Instance.Enqueue(() => ProcessMessage(message));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TCP Receive error: {e.Message}");

            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => Disconnect());
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(message)) return;

            Debug.Log($"Processing: {message.Substring(0, Math.Min(50, message.Length))}...");

            if (message.StartsWith("PLAYERID:"))
            {
                myPlayerId = message.Substring(9).Trim();
                Debug.Log($"✓ Assigned Player ID: {myPlayerId}");
            }
            else if (message.StartsWith("STRING:"))
            {
                string content = message.Substring(7);
                OnTVGotString?.Invoke(content);
            }
            else if (message.StartsWith("IMAGE:"))
            {
                int headerEnd = message.IndexOf('\n');
                if (headerEnd > 0)
                {
                    int imageSize = int.Parse(message.Substring(6, headerEnd - 6));
                    byte[] imageData = Encoding.UTF8.GetBytes(message.Substring(headerEnd + 1));
                    if (imageData.Length >= imageSize)
                    {
                        byte[] trimmedData = new byte[imageSize];
                        Array.Copy(imageData, 0, trimmedData, 0, imageSize);
                        OnTVGotNude?.Invoke(myPlayerId, trimmedData);
                    }
                }
            }
            else if (message.StartsWith("AVATAR:"))
            {
                int headerEnd = message.IndexOf('\n');
                if (headerEnd > 0)
                {
                    int imageSize = int.Parse(message.Split(':')[2]);
                    byte[] imageData = Encoding.UTF8.GetBytes(message.Substring(headerEnd + 1));
                    if (imageData.Length >= imageSize)
                    {
                        byte[] trimmedData = new byte[imageSize];
                        Array.Copy(imageData, 0, trimmedData, 0, imageSize);
                        OnGotAvatar?.Invoke(myPlayerId, trimmedData, message.Split(':')[1]);
                    }
                }
            }
            else if (message.StartsWith("FROMNUDE:"))
            {
                int firstColon = message.IndexOf(':', 9);
                string senderId = message.Substring(9, firstColon - 9);
                string content = message.Substring(firstColon + 1);
                GotNudeFrom?.Invoke(senderId, content);
            }
            else if (message.StartsWith("PLAYERNUDE:"))
            {
                int firstColon = message.IndexOf(':', 11);
                int headerEnd = message.IndexOf('\n');
                string senderId = message.Substring(11, firstColon - 11);
                int imageSize = int.Parse(message.Substring(firstColon + 1, headerEnd - firstColon - 1));
                byte[] imageData = Encoding.UTF8.GetBytes(message.Substring(headerEnd + 1));
                if (imageData.Length >= imageSize)
                {
                    byte[] trimmedData = new byte[imageSize];
                    Array.Copy(imageData, 0, trimmedData, 0, imageSize);
                    OnTVGotNude?.Invoke(senderId, trimmedData);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing message: {e.Message}\n{e.StackTrace}");
        }
    }

    public void SendInput(string inputState)
    {
        SendToTV($"INPUT:{inputState}");
    }

    public void SendGyroData(Vector3 gyro, Vector3 accel)
    {
        string gyroData = $"{gyro.x:F2},{gyro.y:F2},{gyro.z:F2},{accel.x:F2},{accel.y:F2},{accel.z:F2}";
        SendToTV($"GYRO:{gyroData}");
    }

    public void SendMousePosition(Vector2 position)
    {
        SendToTV($"MOUSE:{position.x:F2},{position.y:F2}");
    }

    public void SendStringToTV(string message)
    {
        SendToTV($"STRING:{message}");
    }

    public void SendImageToTV(byte[] imageData)
    {
        byte[] header = Encoding.UTF8.GetBytes($"IMAGE:{imageData.Length}\n");
        byte[] fullMessage = new byte[header.Length + imageData.Length];
        Array.Copy(header, 0, fullMessage, 0, header.Length);
        Array.Copy(imageData, 0, fullMessage, header.Length, imageData.Length);
        SendToTV(fullMessage);
    }

    public void SendImageToPlayer(string targetPlayerId, byte[] imageData)
    {
        byte[] header = Encoding.UTF8.GetBytes($"TOIMGNUDE:{targetPlayerId}:{imageData.Length}\n");
        byte[] fullMessage = new byte[header.Length + imageData.Length];
        Array.Copy(header, 0, fullMessage, 0, header.Length);
        Array.Copy(imageData, 0, fullMessage, header.Length, imageData.Length);
        SendToTV(fullMessage);
    }

    public void SendTextureToTV(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null");
            return;
        }
        byte[] imageData = texture.EncodeToPNG();
        SendImageToTV(imageData);
    }

    public string[] GetAllThoseBitches()
    {
        SendToTV("GETDEVICES");
        return new string[0];
    }

    public void SendMyNudeTo(string targetPlayerId, string message)
    {
        SendToTV($"TONUDE:{targetPlayerId}:{message}");
    }

    public void SendMyMessageTo(string targetPlayerId, string message)
    {
        SendToTV($"FORWARD:{targetPlayerId}:{message}");
    }

    private void SendToTV(string message)
    {
        SendToTV(Encoding.UTF8.GetBytes(message));
    }

    private void SendToTV(byte[] data)
    {
        if (!isConnected)
        {
            Debug.LogWarning("Cannot send - not connected");
            return;
        }

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (webSocketId >= 0)
            {
                WebSocketSend(webSocketId, data, data.Length);
                Debug.Log($"WebGL sent: {Encoding.UTF8.GetString(data).Substring(0, Math.Min(50, data.Length))}...");
            }
#else
            if (isWebGL)
            {
                if (wsClient != null && wsClient.IsAlive)
                {
                    wsClient.Send(data);
                    Debug.Log($"WebSocket sent: {Encoding.UTF8.GetString(data).Substring(0, Math.Min(50, data.Length))}...");
                }
            }
            else
            {
                if (tcpStream != null)
                {
                    tcpStream.Write(data, 0, data.Length);
                    tcpStream.Flush();
                    Debug.Log($"TCP sent: {Encoding.UTF8.GetString(data).Substring(0, Math.Min(50, data.Length))}...");
                }
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Send error: {e.Message}");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        if (!isConnected && !isRunning) return;

        isConnected = false;
        isRunning = false;

        Debug.Log("Disconnecting...");

#if UNITY_WEBGL && !UNITY_EDITOR
        if (webSocketId >= 0)
        {
            WebSocketClose(webSocketId);
            webSocketId = -1;
        }
#else
        if (isWebGL)
        {
            if (wsClient != null)
            {
                wsClient.Close();
                wsClient = null;
            }
        }
        else
        {
            if (tcpStream != null)
            {
                tcpStream.Close();
                tcpStream = null;
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
        }
#endif

        OnTVFucked?.Invoke();
        Debug.Log("✗ Disconnected from TV");
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        Disconnect();
    }
}