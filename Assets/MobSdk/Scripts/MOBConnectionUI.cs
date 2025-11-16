using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using UnityEngine.SceneManagement;

public class MOBConnectionUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField connectionInput;
    public Button connectButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI debugText; // Add this for on-screen debugging

    [Header("Panels")]
    public GameObject connectionPanel;
    public GameObject controllerPanel;

    [Header("Deep Link Settings")]
    public bool enableDeepLink = true;
    public string deepLinkParameterName = "connect";

    private static bool hasAttemptedAutoConnect = false;
    private static string lastConnectionString = "";
    private float connectionTimer = 0f;
    private bool isConnecting = false;

    private void Start()
    {
        // Subscribe to connection events
        if (MOBConnectionManager.Instance != null)
        {
            MOBConnectionManager.Instance.OnTVConnected += OnConnected;
            MOBConnectionManager.Instance.OnTVFucked += OnDisconnected;
        }
        else
        {
            Debug.LogError("[MOBConnectionUI] MOBConnectionManager.Instance is null!");
            AddDebugLog("ERROR: ConnectionManager is null!");
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        ShowConnectionPanel();
        CheckSecurityContext();

        if (enableDeepLink)
        {
            CheckDeepLinkAndConnect();
        }
        else if (!string.IsNullOrEmpty(lastConnectionString))
        {
            // If deep link is disabled but we have a stored connection string (from reconnect), use it
            AddDebugLog("Reconnecting with stored connection string");
            if (connectionInput != null)
            {
                connectionInput.text = lastConnectionString;
            }
            AttemptConnection(lastConnectionString);
        }
    }

    private void Update()
    {
        // Connection timeout detection
        if (isConnecting)
        {
            connectionTimer += Time.deltaTime;

            if (connectionTimer > 10f) // 10 second timeout
            {
                Debug.LogError("[MOBConnectionUI] Connection timeout!");
                AddDebugLog("Connection timeout after 10 seconds");
                SetStatus("Connection timeout! Check TV is running and on same network.", Color.red);
                isConnecting = false;
                connectionTimer = 0f;
            }
        }
    }

    private void CheckDeepLinkAndConnect()
    {
        if (hasAttemptedAutoConnect && string.IsNullOrEmpty(lastConnectionString))
        {
            Debug.Log("[MOBConnectionUI] Auto-connect already attempted and no stored connection");
            return;
        }

        // If we have a stored connection string (from reconnect), don't extract from URL again
        if (!string.IsNullOrEmpty(lastConnectionString))
        {
            Debug.Log("[MOBConnectionUI] Using stored connection string for reconnect");
            AddDebugLog("Reconnecting...");

            if (connectionInput != null)
            {
                connectionInput.text = lastConnectionString;
            }

            SetStatus("Reconnecting...", Color.cyan);
            AttemptConnection(lastConnectionString);
            return;
        }

        hasAttemptedAutoConnect = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string currentUrl = Application.absoluteURL;
            
            AddDebugLog($"URL: {currentUrl}");
            
            if (string.IsNullOrEmpty(currentUrl))
            {
                Debug.LogWarning("[MOBConnectionUI] Current URL is empty");
                AddDebugLog("WARNING: URL is empty");
                return;
            }

            Debug.Log($"[MOBConnectionUI] Current URL: {currentUrl}");

            Uri uri = new Uri(currentUrl);
            string query = uri.Query;

            if (string.IsNullOrEmpty(query))
            {
                Debug.Log("[MOBConnectionUI] No query parameters found");
                AddDebugLog("No query parameters found");
                return;
            }

            AddDebugLog($"Query: {query}");

            string[] parameters = query.TrimStart('?').Split('&');
            
            foreach (string param in parameters)
            {
                string[] keyValue = param.Split('=');
                
                if (keyValue.Length == 2 && keyValue[0].Equals(deepLinkParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    string encodedData = keyValue[1];
                    Debug.Log($"[MOBConnectionUI] Found parameter: {encodedData}");
                    AddDebugLog($"Encoded: {encodedData}");
                    
                    if (TryDecodeConnectionData(encodedData, out string connectionString))
                    {
                        Debug.Log($"[MOBConnectionUI] Decoded: {connectionString}");
                        AddDebugLog($"Decoded: {connectionString}");
                        
                        // Store the connection string for reconnect scenarios
                        lastConnectionString = connectionString;
                        
                        if (connectionInput != null)
                        {
                            connectionInput.text = connectionString;
                        }
                        
                        SetStatus("Auto-connecting...", Color.cyan);
                        AttemptConnection(connectionString);
                        return;
                    }
                    else
                    {
                        Debug.LogError($"[MOBConnectionUI] Failed to decode");
                        AddDebugLog("ERROR: Failed to decode");
                        SetStatus("Invalid QR code data", Color.red);
                    }
                }
            }

            Debug.Log("[MOBConnectionUI] No valid parameter found");
            AddDebugLog("No valid 'connect' parameter");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBConnectionUI] Error: {e.Message}\n{e.StackTrace}");
            AddDebugLog($"ERROR: {e.Message}");
        }
#else
        Debug.Log("[MOBConnectionUI] Deep link only works in WebGL");
        AddDebugLog("Running in Editor - deep link disabled");
#endif
    }

    private bool TryDecodeConnectionData(string encodedData, out string connectionString)
    {
        connectionString = "";

        try
        {
            encodedData = Uri.UnescapeDataString(encodedData);
            Debug.Log($"[MOBConnectionUI] URL decoded: {encodedData}");

            byte[] data = Convert.FromBase64String(encodedData);
            connectionString = Encoding.UTF8.GetString(data);

            Debug.Log($"[MOBConnectionUI] Base64 decoded: {connectionString}");

            if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains(":"))
            {
                Debug.LogError($"[MOBConnectionUI] Invalid format: {connectionString}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBConnectionUI] Decode error: {e.Message}");
            AddDebugLog($"Decode error: {e.Message}");
            return false;
        }
    }

    private void AttemptConnection(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            SetStatus("Invalid connection data", Color.red);
            AddDebugLog("ERROR: Empty connection string");
            return;
        }

        // Store the connection string for potential reconnect
        lastConnectionString = connectionString;

        if (ParseConnectionString(connectionString, out string ip, out int port))
        {
            SetStatus($"Connecting to {ip}:{port}...", Color.cyan);
            AddDebugLog($"Attempting: {ip}:{port}");
            Debug.Log($"[MOBConnectionUI] Connecting to IP: {ip}, Port: {port}");

            isConnecting = true;
            connectionTimer = 0f;

            if (MOBConnectionManager.Instance != null)
            {
                MOBConnectionManager.Instance.ConnectToTV(ip, port);
            }
            else
            {
                Debug.LogError("[MOBConnectionUI] ConnectionManager is null!");
                AddDebugLog("ERROR: ConnectionManager null");
                SetStatus("Connection Manager not found!", Color.red);
                isConnecting = false;
            }
        }
        else
        {
            SetStatus("Invalid connection format!", Color.red);
            AddDebugLog($"ERROR: Parse failed for '{connectionString}'");
            Debug.LogError($"[MOBConnectionUI] Failed to parse: {connectionString}");
            isConnecting = false;
        }
    }

    private void CheckSecurityContext()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string currentUrl = Application.absoluteURL;
        
        if (!string.IsNullOrEmpty(currentUrl) && currentUrl.StartsWith("https://"))
        {
            Debug.LogWarning("[MOBConnectionUI] HTTPS detected!");
            AddDebugLog("WARNING: HTTPS detected - WS blocked!");
            SetStatus("⚠️ HTTPS detected! Use HTTP version.", Color.yellow);
        }
        else
        {
            Debug.Log($"[MOBConnectionUI] HTTP/file - WebSocket OK");
            AddDebugLog("Protocol: OK (HTTP)");
        }
#endif
    }

    public void OnConnectButtonClicked()
    {
        if (connectionInput == null)
        {
            Debug.LogError("[MOBConnectionUI] Input field null!");
            AddDebugLog("ERROR: Input field null");
            SetStatus("Error: Input field not found", Color.red);
            return;
        }

        string input = connectionInput.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            SetStatus("Please enter IP:Port", Color.yellow);
            Debug.LogWarning("[MOBConnectionUI] Empty input");
            return;
        }

        Debug.Log($"[MOBConnectionUI] Manual connect: {input}");
        AddDebugLog($"Manual: {input}");
        AttemptConnection(input);
    }

    private bool ParseConnectionString(string input, out string ip, out int port)
    {
        ip = "";
        port = 0;

        try
        {
            input = input.Trim();

            if (input.StartsWith("ws://") || input.StartsWith("wss://"))
            {
                string urlWithoutProtocol = input.StartsWith("ws://") ? input.Substring(5) : input.Substring(6);
                int colonIndex = urlWithoutProtocol.IndexOf(':');

                if (colonIndex > 0)
                {
                    ip = urlWithoutProtocol.Substring(0, colonIndex);
                    int slashIndex = urlWithoutProtocol.IndexOf('/', colonIndex);
                    string portString = slashIndex > 0
                        ? urlWithoutProtocol.Substring(colonIndex + 1, slashIndex - colonIndex - 1)
                        : urlWithoutProtocol.Substring(colonIndex + 1);

                    port = int.Parse(portString);
                    return true;
                }
            }
            else if (input.Contains(":"))
            {
                string[] parts = input.Split(':');

                if (parts.Length >= 2)
                {
                    ip = parts[0].Trim();
                    string portPart = parts[1].Trim();

                    int endIndex = 0;
                    while (endIndex < portPart.Length && char.IsDigit(portPart[endIndex]))
                    {
                        endIndex++;
                    }

                    if (endIndex > 0)
                    {
                        portPart = portPart.Substring(0, endIndex);
                    }

                    port = int.Parse(portPart);

                    string[] ipParts = ip.Split('.');
                    if (ipParts.Length != 4)
                    {
                        AddDebugLog($"Invalid IP: {ip}");
                        return false;
                    }

                    if (port <= 0 || port > 65535)
                    {
                        AddDebugLog($"Invalid port: {port}");
                        return false;
                    }

                    return true;
                }
            }
            else
            {
                ip = input.Trim();
                string[] ipParts = ip.Split('.');

                if (ipParts.Length != 4)
                {
                    return false;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                port = 7778;
#else
                port = Application.platform == RuntimePlatform.WebGLPlayer ? 7778 : 7777;
#endif
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBConnectionUI] Parse error: {e.Message}");
            AddDebugLog($"Parse error: {e.Message}");
        }

        return false;
    }

    public void OnReconnectButtonClicked()
    {
        Debug.Log("[MOBConnectionUI] Reconnect button clicked");
        AddDebugLog("Reconnecting...");

        // Don't reset the static variables - they will persist across scene reload
        // Just reload the scene and the Start() method will use the stored connection string
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnConnected()
    {
        Debug.Log("[MOBConnectionUI] Connection successful!");
        AddDebugLog("✓ CONNECTED!");
        SetStatus("Connected!", Color.green);

        isConnecting = false;
        connectionTimer = 0f;

        Invoke(nameof(ShowControllerPanel), 1f);
    }

    private void OnDisconnected()
    {
        Debug.Log("[MOBConnectionUI] Disconnected!");
        AddDebugLog("✗ Disconnected");
        SetStatus("Disconnected! Check IP/Port.", Color.red);

        isConnecting = false;
        connectionTimer = 0f;

        ShowConnectionPanel();
    }

    private void ShowConnectionPanel()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(true);

        if (controllerPanel != null)
            controllerPanel.SetActive(false);

        SetStatus("Enter TV IP:Port to connect", Color.white);
    }

    private void ShowControllerPanel()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(false);

        if (controllerPanel != null)
            controllerPanel.SetActive(true);
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }

        Debug.Log($"[MOBConnectionUI] Status: {message}");
    }

    private void AddDebugLog(string message)
    {
        if (debugText != null)
        {
            debugText.text += $"\n{message}";

            // Keep only last 10 lines
            string[] lines = debugText.text.Split('\n');
            if (lines.Length > 10)
            {
                debugText.text = string.Join("\n", lines, lines.Length - 10, 10);
            }
        }

        Debug.Log($"[DEBUG] {message}");
    }

    private void OnDestroy()
    {
        if (MOBConnectionManager.Instance != null)
        {
            MOBConnectionManager.Instance.OnTVConnected -= OnConnected;
            MOBConnectionManager.Instance.OnTVFucked -= OnDisconnected;
        }

        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(OnConnectButtonClicked);
        }
    }
}