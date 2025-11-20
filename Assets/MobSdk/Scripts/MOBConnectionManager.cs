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
    public int tcpPort = 7777;
    public int wsPort = 7778;
    public string wsPath = "/mobile";

    [Header("Auto-Reconnect Settings")]
    public int maxReconnectAttempts = 5; // REDUCED from 10
    public float reconnectInterval = 3f; // INCREASED from 2f
    public float reconnectTimeout = 15f; // REDUCED from 20f
    public float pingInterval = 5f; // INCREASED from 3f - reduce ping frequency

    private TcpClient tcpClient;
    private NetworkStream tcpStream;

#if !UNITY_WEBGL || UNITY_EDITOR
    private WebSocket wsClient;
#else
    private int webSocketId = -1;
    private int previousWebSocketId = -1; // Track previous socket for cleanup
    private Queue<byte[]> messageQueue = new Queue<byte[]>();
#endif

    private Thread receiveThread;
    public bool isConnected = false;
    public string myPlayerId = "";
    private string savedPlayerId = "";
    private bool isRunning = false;
    private bool isWebGL = false;

    // Auto-reconnect state
    private bool isReconnecting = false;
    private int reconnectAttempts = 0;
    private float reconnectTimer = 0f;
    private float totalReconnectTime = 0f;
    private int lastKnownState = -1;
    private float pingTimer = 0f;

    // Connection persistence
    private string lastServerIP = "";
    private int lastServerPort = 0;

    // NEW: Prevent multiple simultaneous reconnection attempts
    private bool reconnectInProgress = false;
    private float timeSinceLastStateChange = 0f;
    private const float MIN_STATE_CHANGE_INTERVAL = 1f; // Minimum 1 second between state changes

    public event Action OnTVConnected;
    public event Action OnTVFucked;
    public event Action OnReconnecting;
    public event Action OnReconnectionFailed;
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

    [DllImport("__Internal")]
    private static extern void WebSocketSetVisibilityCallback(Action<bool> callback);
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
        // Register visibility change callback
        Application.focusChanged += OnApplicationFocusChanged;
#else
        isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
        Debug.Log($"Running in {Application.platform} mode");
#endif

        LoadSavedPlayerId();
    }

    private void LoadSavedPlayerId()
    {
        if (PlayerPrefs.HasKey("SavedPlayerID"))
        {
            savedPlayerId = PlayerPrefs.GetString("SavedPlayerID");
            Debug.Log($"[MOBConnectionManager] Loaded saved Player ID: {savedPlayerId}");
        }
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    private void OnApplicationFocusChanged(bool hasFocus)
    {
        Debug.Log($"[MOBConnectionManager] App focus changed: {hasFocus}");

        if (!hasFocus)
        {
            // App lost focus - connection may suspend
            Debug.LogWarning("[MOBConnectionManager] App backgrounded - connection may be suspended");
        }
        else
        {
            // App regained focus - check connection
            Debug.Log("[MOBConnectionManager] App foregrounded - checking connection");

            if (isConnected && webSocketId >= 0)
            {
                int state = WebSocketGetState(webSocketId);
                if (state != 1) // Not OPEN
                {
                    Debug.LogWarning($"[MOBConnectionManager] Connection not open after resume (state: {state}) - reconnecting");
                    isConnected = false;
                    AttemptReconnect();
                }
                else
                {
                    // Send ping to verify connection is alive
                    SendToTV("PING");
                }
            }
        }
    }
#endif
    private void SavePlayerId(string playerId)
    {
        savedPlayerId = playerId;
        myPlayerId = playerId;
        PlayerPrefs.SetString("SavedPlayerID", playerId);
        PlayerPrefs.Save();
        Debug.Log($"[MOBConnectionManager] Saved Player ID: {playerId}");
    }

    public void ConnectToTV(string ip, int port)
    {
        // Prevent connection attempts while already connecting/reconnecting
        if (reconnectInProgress)
        {
            Debug.LogWarning("[MOBConnectionManager] Connection attempt ignored - reconnection already in progress");
            return;
        }

        serverIP = ip;
        lastServerIP = ip;
        lastServerPort = port;

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

            ResetReconnectionState();

            receiveThread = new Thread(ReceiveTcpMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"✓ TCP Connected to TV at {serverIP}:{tcpPort}");

            if (!string.IsNullOrEmpty(savedPlayerId))
            {
                SendToTV($"RECONNECT:{savedPlayerId}");
                Debug.Log($"Sent reconnection request with ID: {savedPlayerId}");
            }

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
                UnityMainThreadDispatcher.Instance.Enqueue(() => AttemptReconnect());
            }
        }
    }

    private void ConnectWebSocket()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // CRITICAL FIX: Close previous websocket before creating new one
            if (webSocketId >= 0)
            {
                Debug.Log($"[MOBConnectionManager] Closing previous WebSocket ID {webSocketId} before creating new one");
                try
                {
                    WebSocketClose(webSocketId);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error closing previous WebSocket: {e.Message}");
                }
                previousWebSocketId = webSocketId;
                webSocketId = -1;
            }

            string url = $"ws://{serverIP}:{wsPort}{wsPath}";
            Debug.Log($"Attempting WebGL WebSocket connection to: {url}");
            
            int newSocketId = WebSocketConnect(url);
            
            if (newSocketId >= 0)
            {
                webSocketId = newSocketId;
                isRunning = true;
                lastKnownState = 0; // CONNECTING
                timeSinceLastStateChange = 0f;
                Debug.Log($"✓ WebSocket connection initiated with ID: {webSocketId}");
            }
            else
            {
                Debug.LogError($"✗ WebSocket Connect failed: Invalid socket ID");
                isConnected = false;
                
                if (!isReconnecting && !reconnectInProgress)
                {
                    AttemptReconnect();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ WebSocket Connect failed: {e.Message}\n{e.StackTrace}");
            isConnected = false;
            
            if (!isReconnecting && !reconnectInProgress)
            {
                AttemptReconnect();
            }
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

                ResetReconnectionState();

                Debug.Log($"✓ WebSocket Connected to TV at {url}");

                if (!string.IsNullOrEmpty(savedPlayerId))
                {
                    SendToTV($"RECONNECT:{savedPlayerId}");
                    Debug.Log($"Sent reconnection request with ID: {savedPlayerId}");
                }

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
                    UnityMainThreadDispatcher.Instance.Enqueue(() => AttemptReconnect());
                }
            };

            wsClient.OnClose += (sender, e) =>
            {
                Debug.Log($"WebSocket closed: Code={e.Code}, Reason={e.Reason}");

                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() => AttemptReconnect());
                }
            };

            wsClient.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ WebSocket Connect failed: {e.Message}\n{e.StackTrace}");
            isConnected = false;

            if (!isReconnecting && !reconnectInProgress)
            {
                AttemptReconnect();
            }
        }
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void Update()
    {
        timeSinceLastStateChange += Time.deltaTime;

        // Periodic PING to keep connection alive (only when stable and connected)
        if (isConnected && !isReconnecting && !reconnectInProgress)
        {
            pingTimer += Time.deltaTime;
            if (pingTimer >= pingInterval)
            {
                pingTimer = 0f;
                SendToTV("PING");
            }
        }

        // Handle reconnection logic
        if (isReconnecting)
        {
            reconnectTimer += Time.deltaTime;
            totalReconnectTime += Time.deltaTime;

            if (totalReconnectTime >= reconnectTimeout)
            {
                Debug.LogError($"Reconnection timeout after {reconnectTimeout} seconds");
                FinalizeDisconnection();
                return;
            }

            if (reconnectTimer >= reconnectInterval)
            {
                reconnectTimer = 0f;
                reconnectAttempts++;

                Debug.Log($"Reconnection attempt {reconnectAttempts}/{maxReconnectAttempts}...");

                if (reconnectAttempts >= maxReconnectAttempts)
                {
                    Debug.LogError($"Max reconnection attempts ({maxReconnectAttempts}) reached");
                    FinalizeDisconnection();
                    return;
                }

                // Try to reconnect
                reconnectInProgress = true;
                ConnectWebSocket();
                reconnectInProgress = false;
            }

            return; // Skip normal update logic during reconnection
        }

        // Normal state monitoring (only when NOT reconnecting)
        if (isWebGL && isRunning && webSocketId >= 0)
        {
            int state = WebSocketGetState(webSocketId);

            // State: 0=CONNECTING, 1=OPEN, 2=CLOSING, 3=CLOSED
            if (state == 1 && !isConnected)
            {
                // CRITICAL: Only transition to connected if enough time has passed
                if (timeSinceLastStateChange < MIN_STATE_CHANGE_INTERVAL)
                {
                    Debug.Log($"[MOBConnectionManager] State change to OPEN too soon ({timeSinceLastStateChange:F2}s), waiting...");
                    return;
                }

                isConnected = true;
                lastKnownState = 1;
                timeSinceLastStateChange = 0f;
                
                ResetReconnectionState();
                
                Debug.Log("✓ WebGL WebSocket opened");

                if (!string.IsNullOrEmpty(savedPlayerId))
                {
                    SendToTV($"RECONNECT:{savedPlayerId}");
                    Debug.Log($"Sent reconnection request with ID: {savedPlayerId}");
                }

                OnTVConnected?.Invoke();
            }
            else if (state == 3 && lastKnownState == 1)
            {
                // CRITICAL: Prevent rapid reconnection loops
                if (timeSinceLastStateChange < MIN_STATE_CHANGE_INTERVAL)
                {
                    Debug.LogWarning($"[MOBConnectionManager] WebSocket closed too soon after opening ({timeSinceLastStateChange:F2}s) - possible network instability");
                }

                Debug.LogWarning("WebSocket state changed from OPEN to CLOSED - attempting reconnection");
                lastKnownState = state;
                isConnected = false;
                timeSinceLastStateChange = 0f;
                AttemptReconnect();
            }
            else if (state == 2 && lastKnownState == 1)
            {
                Debug.LogWarning("WebSocket is closing");
                lastKnownState = state;
                timeSinceLastStateChange = 0f;
            }
            else if (state != lastKnownState)
            {
                Debug.Log($"[MOBConnectionManager] WebSocket state: {lastKnownState} → {state}");
                lastKnownState = state;
                timeSinceLastStateChange = 0f;
            }

            // Receive messages (only if connected and state is OPEN)
            if (isConnected && state == 1)
            {
                byte[] buffer = new byte[1024 * 1024];
                int bytesRead = WebSocketReceive(webSocketId, buffer, buffer.Length);

                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessMessage(message);
                }
            }
        }
    }
#else
    private void Update()
    {
        // Periodic PING to keep connection alive (for editor/non-WebGL)
        if (isConnected && !isReconnecting && !isWebGL)
        {
            pingTimer += Time.deltaTime;
            if (pingTimer >= pingInterval)
            {
                pingTimer = 0f;
                SendToTV("PING");
            }
        }
    }
#endif

    private void AttemptReconnect()
    {
        if (isReconnecting || reconnectInProgress)
        {
            Debug.Log("[MOBConnectionManager] Already attempting to reconnect - ignoring duplicate call");
            return;
        }

        Debug.Log("[MOBConnectionManager] Starting automatic reconnection...");
        isReconnecting = true;
        reconnectInProgress = true;
        reconnectAttempts = 0;
        reconnectTimer = 0f;
        totalReconnectTime = 0f;
        isConnected = false;

        OnReconnecting?.Invoke();

        // Close current connection
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webSocketId >= 0)
        {
            try
            {
                Debug.Log($"[MOBConnectionManager] Closing WebSocket {webSocketId} for reconnection");
                WebSocketClose(webSocketId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing WebSocket during reconnect: {e.Message}");
            }
            webSocketId = -1;
        }
#else
        if (isWebGL && wsClient != null)
        {
            try
            {
                wsClient.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing WebSocket during reconnect: {e.Message}");
            }
            wsClient = null;
        }
        else if (!isWebGL && tcpClient != null)
        {
            try
            {
                tcpStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing TCP during reconnect: {e.Message}");
            }
            tcpClient = null;
            tcpStream = null;
        }
#endif

        // Wait a moment before attempting first reconnection
        reconnectTimer = reconnectInterval * 0.5f; // Start halfway to first attempt
        reconnectInProgress = false;
    }

    private void ResetReconnectionState()
    {
        isReconnecting = false;
        reconnectInProgress = false;
        reconnectAttempts = 0;
        reconnectTimer = 0f;
        totalReconnectTime = 0f;
    }

    private void FinalizeDisconnection()
    {
        Debug.LogError("[MOBConnectionManager] Reconnection failed - notifying user");
        ResetReconnectionState();
        OnReconnectionFailed?.Invoke();
        Disconnect();
    }

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
                else
                {
                    Debug.Log("TCP connection closed by server");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TCP Receive error: {e.Message}");
        }
        finally
        {
            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => AttemptReconnect());
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(message)) return;

            // Don't log full message to reduce console spam
            if (message != "PONG" && Time.frameCount % 30 == 0)
            {
                Debug.Log($"Processing: {message.Substring(0, Math.Min(50, message.Length))}...");
            }

            if (message.StartsWith("PLAYERID:"))
            {
                string newPlayerId = message.Substring(9).Trim();
                SavePlayerId(newPlayerId);
                Debug.Log($"✓ Assigned Player ID: {myPlayerId}");
            }
            else if (message.StartsWith("RECONNECT_ACCEPTED:"))
            {
                string confirmedId = message.Substring(19).Trim();
                myPlayerId = confirmedId;
                Debug.Log($"✓ Reconnected with existing ID: {confirmedId}");
            }
            else if (message.StartsWith("RECONNECT_REJECTED"))
            {
                Debug.LogWarning("Reconnection rejected - clearing saved ID");
                savedPlayerId = "";
                PlayerPrefs.DeleteKey("SavedPlayerID");
                PlayerPrefs.Save();
            }
            else if (message == "PONG")
            {
                // Heartbeat response - silent
                return;
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
            // Only log if not PING to reduce spam
            if (data.Length < 4 || Encoding.UTF8.GetString(data, 0, Math.Min(4, data.Length)) != "PING")
            {
                Debug.LogWarning("Cannot send - not connected");
            }
            return;
        }

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (webSocketId >= 0)
            {
                int state = WebSocketGetState(webSocketId);
                if (state == 1) // Only send if OPEN
                {
                    WebSocketSend(webSocketId, data, data.Length);
                }
                else
                {
                    Debug.LogWarning($"Cannot send - WebSocket state is {state}, not OPEN");
                    isConnected = false;
                }
            }
#else
            if (isWebGL)
            {
                if (wsClient != null && wsClient.IsAlive)
                {
                    wsClient.Send(data);
                }
            }
            else
            {
                if (tcpStream != null)
                {
                    tcpStream.Write(data, 0, data.Length);
                    tcpStream.Flush();
                }
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Send error: {e.Message}");
            AttemptReconnect();
        }
    }

    private void Disconnect()
    {
        if (!isConnected && !isRunning && !isReconnecting) return;

        isConnected = false;
        isRunning = false;
        isReconnecting = false;
        reconnectInProgress = false;

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
        isReconnecting = false;
        reconnectInProgress = false;
        Disconnect();
    }

    public bool IsReconnecting()
    {
        return isReconnecting;
    }

    public float GetReconnectionProgress()
    {
        if (!isReconnecting) return 0f;
        return Mathf.Clamp01(totalReconnectTime / reconnectTimeout);
    }

    public void ClearSavedPlayerId()
    {
        savedPlayerId = "";
        myPlayerId = "";
        PlayerPrefs.DeleteKey("SavedPlayerID");
        PlayerPrefs.Save();
        Debug.Log("[MOBConnectionManager] Cleared saved Player ID");
    }
}