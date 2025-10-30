using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Runtime.InteropServices;

public class MOBDataSender : MonoBehaviour
{
    [Header("UI References")]
    public Button btnPickImage;
    public TMP_InputField messageInputField;
    public Button btnSendMessage;
    public Button btnOpenMessagePanel;
    public TextMeshProUGUI statusText;
    public GameObject MessagePanel;

    [Header("Settings")]
    public Color successColor = Color.green;
    public Color errorColor = Color.red;
    public Color normalColor = Color.white;
    public float statusDisplayDuration = 2f;

    private MOBConnectionManager connectionManager;
    private float statusTimer = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenFilePicker(string gameObjectName, string callbackMethod);
#endif

    private void Start()
    {
        connectionManager = MOBConnectionManager.Instance;

        if (connectionManager == null)
        {
            Debug.LogError("[MOBDataSender] MOBConnectionManager.Instance is null!");
            SetStatus("Error: Connection Manager not found!", errorColor);
            return;
        }

        // Setup button listeners
        if (btnPickImage != null)
        {
            btnPickImage.onClick.AddListener(OnPickImageClicked);
        }
        else
        {
            Debug.LogWarning("[MOBDataSender] Pick Image button is not assigned!");
        }

        if (btnSendMessage != null)
        {
            btnSendMessage.onClick.AddListener(OnSendMessageClicked);
        }
        else
        {
            Debug.LogWarning("[MOBDataSender] Send Message button is not assigned!");
        }

        if (btnOpenMessagePanel != null)
        {
            btnOpenMessagePanel.onClick.AddListener(OnOpenMessagePanel);
        }
        else
        {
            Debug.LogWarning("[MOBDataSender] Send Message button is not assigned!");
        }

        // Setup input field enter key
        if (messageInputField != null)
        {
            messageInputField.onSubmit.AddListener((text) => OnSendMessageClicked());
        }
        else
        {
            Debug.LogWarning("[MOBDataSender] Message input field is not assigned!");
        }

        SetStatus("Ready to send data", normalColor);
    }

    private void Update()
    {
        // Auto-hide status message after duration
        if (statusTimer > 0f)
        {
            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0f && statusText != null)
            {
                statusText.text = "Ready to send data";
                statusText.color = normalColor;
            }
        }

        // Update button interactivity based on connection
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool isConnected = connectionManager != null && connectionManager.isConnected;

        if (btnPickImage != null)
        {
            btnPickImage.interactable = isConnected;
        }

        if (btnSendMessage != null)
        {
            bool hasText = messageInputField != null && !string.IsNullOrWhiteSpace(messageInputField.text);
            btnSendMessage.interactable = isConnected && hasText;
        }
    }

    // BUTTON: Pick Image
    public void OnPickImageClicked()
    {
        if (!CheckConnection()) return;

        Debug.Log("[MOBDataSender] Opening file picker...");
        SetStatus("Opening file picker...", normalColor);

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            OpenFilePicker(gameObject.name, "OnImageLoaded");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBDataSender] Failed to open file picker: {e.Message}");
            SetStatus("Failed to open file picker!", errorColor);
        }
#else
        Debug.LogWarning("[MOBDataSender] File picker only works in WebGL builds!");
        SetStatus("File picker only works on WebGL", errorColor);
#endif
    }

    // CALLBACK: Image loaded from file picker
    public void OnImageLoaded(string data)
    {
        try
        {
            Debug.Log($"[MOBDataSender] OnImageLoaded called with data: {data.Substring(0, Math.Min(50, data.Length))}...");

            // Parse the data: "pointer,length"
            string[] parts = data.Split(',');
            if (parts.Length != 2)
            {
                Debug.LogError($"[MOBDataSender] Invalid data format: {data}");
                SetStatus("Error: Invalid image data", errorColor);
                return;
            }

            int pointer = int.Parse(parts[0]);
            int length = int.Parse(parts[1]);

            Debug.Log($"[MOBDataSender] Extracting {length} bytes from pointer {pointer}");

            // Extract bytes from unmanaged memory
            byte[] imageBytes = new byte[length];
            Marshal.Copy((IntPtr)pointer, imageBytes, 0, length);

            Debug.Log($"[MOBDataSender] Extracted {imageBytes.Length} bytes");

            // Validate image data
            if (imageBytes.Length < 100)
            {
                Debug.LogError("[MOBDataSender] Image data too small!");
                SetStatus("Error: Invalid image file", errorColor);
                return;
            }

            // Send to TV
            SetStatus("Sending image to TV...", normalColor);
            connectionManager.SendImageToTV(imageBytes);

            Debug.Log($"[MOBDataSender] ✓ Image sent successfully! ({imageBytes.Length} bytes)");
            SetStatus($"Image sent! ({FormatFileSize(imageBytes.Length)})", successColor);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBDataSender] Error processing image: {e.Message}\n{e.StackTrace}");
            SetStatus("Error sending image!", errorColor);
        }
    }

    // BUTTON: Send Message
    public void OnSendMessageClicked()
    {
        if (!CheckConnection()) return;

        if (messageInputField == null)
        {
            Debug.LogError("[MOBDataSender] Message input field is null!");
            SetStatus("Error: Input field not found!", errorColor);
            return;
        }

        string message = messageInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("[MOBDataSender] Message is empty!");
            SetStatus("Message is empty!", errorColor);
            return;
        }

        try
        {
            Debug.Log($"[MOBDataSender] Sending message: {message}");
            connectionManager.SendStringToTV(message);

            SetStatus($"Message sent!", successColor);
            Debug.Log($"[MOBDataSender] ✓ Message sent successfully: {message}");

            // Clear input field after sending
            messageInputField.text = "";
        }
        catch (Exception e)
        {
            Debug.LogError($"[MOBDataSender] Error sending message: {e.Message}\n{e.StackTrace}");
            SetStatus("Error sending message!", errorColor);
        }
    }

    public void OnOpenMessagePanel()
    {
        MessagePanel.SetActive(!MessagePanel.activeInHierarchy);
    }

    // Helper: Check connection
    private bool CheckConnection()
    {
        if (connectionManager == null)
        {
            Debug.LogError("[MOBDataSender] Connection Manager is null!");
            SetStatus("Error: Connection Manager missing!", errorColor);
            return false;
        }

        if (!connectionManager.isConnected)
        {
            Debug.LogWarning("[MOBDataSender] Not connected to TV!");
            SetStatus("Not connected to TV!", errorColor);
            return false;
        }

        return true;
    }

    // Helper: Set status text
    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
            statusTimer = statusDisplayDuration;
        }

        Debug.Log($"[MOBDataSender] Status: {message}");
    }

    // Helper: Format file size
    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:F1} KB";
        else
            return $"{bytes / (1024f * 1024f):F1} MB";
    }

    private void OnDestroy()
    {
        if (btnPickImage != null)
        {
            btnPickImage.onClick.RemoveListener(OnPickImageClicked);
        }

        if (btnSendMessage != null)
        {
            btnSendMessage.onClick.RemoveListener(OnSendMessageClicked);
        }
    }
}