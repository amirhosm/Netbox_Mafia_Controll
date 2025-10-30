using UnityEngine;
using UnityEngine.UI;
using ZXing;
using TMPro;
using UnityEngine.Android;
using System;

public class MOBQRScanner : MonoBehaviour
{
    [Header("UI References")]
    public RawImage cameraDisplay;
    public GameObject cameraDisplayPanel; // Add reference to the panel GameObject
    public Button scanButton;
    public Button captureButton;
    public TextMeshProUGUI statusText;
    public GameObject scannerPanel;
    public GameObject connectionPanel;

    public TMP_InputField ipInput;
    public TMP_InputField portInput;

    private WebCamTexture webcamTexture;
    private bool isScanning = false;
    private bool useRearCamera = true;

    private void Start()
    {
        if (scanButton != null)
            scanButton.onClick.AddListener(StartScanning);

        if (captureButton != null)
            captureButton.onClick.AddListener(CaptureAndScan);
            
        // Subscribe to connection events
        if (MOBConnectionManager.Instance != null)
        {
            MOBConnectionManager.Instance.OnTVConnected += OnConnectionSuccess;
            MOBConnectionManager.Instance.OnTVFucked += OnConnectionFailed;
        }
        
        // Make sure camera display is hidden at start
        if (cameraDisplayPanel != null)
        {
            cameraDisplayPanel.SetActive(false);
        }
    }

    public void StartScanning()
    {
        if (isScanning)
        {
            StopScanning();
            return;
        }

        if (connectionPanel != null)
            connectionPanel.SetActive(false);

        if (scannerPanel != null)
            scannerPanel.SetActive(true);
            
        // Show the camera display panel
        if (cameraDisplayPanel != null)
        {
            cameraDisplayPanel.SetActive(true);
            Debug.Log("[QRScanner] Camera display panel activated");
        }

        statusText.text = "Initializing camera...";

        // Request camera permission on WebGL and Android
#if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(RequestCameraPermissionWebGL());
#elif UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            statusText.text = "Requesting camera permission...";
            Invoke(nameof(CheckPermissionAndStart), 1f);
            return;
        }
        InitializeCamera();
#else
        InitializeCamera();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private System.Collections.IEnumerator RequestCameraPermissionWebGL()
    {
        yield return null;
        InitializeCamera();
    }
#endif

    private void CheckPermissionAndStart()
    {
#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            InitializeCamera();
        }
        else
        {
            statusText.text = "Camera permission denied!";
            
            // Hide camera display if permission denied
            if (cameraDisplayPanel != null)
            {
                cameraDisplayPanel.SetActive(false);
            }
            
            if (scannerPanel != null)
                scannerPanel.SetActive(false);
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
        }
#else
        InitializeCamera();
#endif
    }

    private void InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            statusText.text = "No camera found!";
            Debug.LogError("[QRScanner] No camera devices found");
            
            // Hide camera display if no camera
            if (cameraDisplayPanel != null)
            {
                cameraDisplayPanel.SetActive(false);
            }
            return;
        }

        Debug.Log($"[QRScanner] Found {devices.Length} camera(s)");
        
        // List all cameras
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[QRScanner] Camera {i}: {devices[i].name}, Front-facing: {devices[i].isFrontFacing}");
        }

        WebCamDevice selectedCamera = devices[0];
        bool foundRearCamera = false;

        // Try to find rear camera
        if (useRearCamera)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (!devices[i].isFrontFacing)
                {
                    selectedCamera = devices[i];
                    foundRearCamera = true;
                    Debug.Log($"[QRScanner] Selected REAR camera: {selectedCamera.name}");
                    break;
                }
            }
        }

        // If no rear camera found, try to select by name (common patterns)
        if (!foundRearCamera && devices.Length > 1)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                string lowerName = devices[i].name.ToLower();
                
                // Common rear camera naming patterns
                if (lowerName.Contains("back") || 
                    lowerName.Contains("rear") || 
                    lowerName.Contains("camera2") ||
                    lowerName.Contains("environment") ||
                    (devices.Length > 1 && i == 1))
                {
                    selectedCamera = devices[i];
                    Debug.Log($"[QRScanner] Selected camera by name pattern: {selectedCamera.name}");
                    break;
                }
            }
        }

        if (!foundRearCamera && useRearCamera)
        {
            Debug.LogWarning($"[QRScanner] No rear camera detected, using: {selectedCamera.name}");
        }

        // Create WebCamTexture with selected camera
        webcamTexture = new WebCamTexture(selectedCamera.name, 1280, 720, 30);
        
        if (cameraDisplay != null)
        {
            cameraDisplay.texture = webcamTexture;
            
            // Adjust rotation based on platform
#if UNITY_ANDROID && !UNITY_EDITOR
            cameraDisplay.transform.localRotation = Quaternion.Euler(0, 0, -90);
#elif UNITY_WEBGL && !UNITY_EDITOR
            cameraDisplay.transform.localRotation = Quaternion.identity;
#endif
        }

        webcamTexture.Play();
        
        Debug.Log($"[QRScanner] Camera started: {webcamTexture.deviceName}, Playing: {webcamTexture.isPlaying}");

        isScanning = true;
        statusText.text = "Point at QR code";

        if (captureButton != null)
            captureButton.interactable = true;

        // Start scanning automatically
        InvokeRepeating(nameof(ScanQRCode), 0.5f, 0.5f);
    }

    public void SwitchCamera()
    {
        useRearCamera = !useRearCamera;
        
        if (isScanning)
        {
            StopScanning();
            StartScanning();
        }
    }

    public void CaptureAndScan()
    {
        statusText.text = "Processing...";

        // For manual input connection
        if (!string.IsNullOrEmpty(ipInput.text))
        {
            // Hide camera display when connecting manually
            if (cameraDisplayPanel != null)
            {
                cameraDisplayPanel.SetActive(false);
                Debug.Log("[QRScanner] Camera display panel deactivated (manual connect)");
            }
            
            ProcessQRCode(ipInput.text);
        }
        else if (webcamTexture != null && webcamTexture.isPlaying)
        {
            // Capture from camera - keep display visible for now
            ScanQRCode();
        }
        else
        {
            statusText.text = "Please enter connection info or scan QR code";
        }
    }

    private void ScanQRCode()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return;

        try
        {
            IBarcodeReader barcodeReader = new BarcodeReader();
            var result = barcodeReader.Decode(webcamTexture.GetPixels32(), webcamTexture.width, webcamTexture.height);

            if (result != null)
            {
                Debug.Log($"[QRScanner] QR Code detected: {result.Text}");
                statusText.text = "QR Code found!";
                
                // Hide camera display when QR code is found
                if (cameraDisplayPanel != null)
                {
                    cameraDisplayPanel.SetActive(false);
                    Debug.Log("[QRScanner] Camera display panel deactivated (QR found)");
                }
                
                ProcessQRCode(result.Text);
                StopScanning();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[QRScanner] Error scanning: {e.Message}");
        }
    }

    private void ProcessQRCode(string qrData)
    {
        try
        {
            string ip = "";
            int port = 0;

            // Check if it's a WebSocket URL format: ws://192.168.1.100:7778/mobile
            if (qrData.StartsWith("ws://") || qrData.StartsWith("wss://"))
            {
                Debug.Log($"[QRScanner] Detected WebSocket URL: {qrData}");

                string urlWithoutProtocol = qrData.StartsWith("ws://") ? qrData.Substring(5) : qrData.Substring(6);

                int colonIndex = urlWithoutProtocol.IndexOf(':');
                if (colonIndex > 0)
                {
                    ip = urlWithoutProtocol.Substring(0, colonIndex);

                    int slashIndex = urlWithoutProtocol.IndexOf('/', colonIndex);
                    if (slashIndex > 0)
                    {
                        string portString = urlWithoutProtocol.Substring(colonIndex + 1, slashIndex - colonIndex - 1);
                        port = int.Parse(portString);
                    }
                    else
                    {
                        string portString = urlWithoutProtocol.Substring(colonIndex + 1);
                        port = int.Parse(portString);
                    }
                }
            }
            // Simple format: 192.168.1.100:7778
            else if (qrData.Contains(":"))
            {
                Debug.Log($"[QRScanner] Detected IP:Port format: {qrData}");
                string[] parts = qrData.Split(':');
                if (parts.Length >= 2)
                {
                    ip = parts[0].Trim();
                    
                    string portPart = parts[1].Trim();
                    int slashIndex = portPart.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        portPart = portPart.Substring(0, slashIndex);
                    }
                    
                    port = int.Parse(portPart);
                }
            }
            // Just IP address
            else if (!string.IsNullOrEmpty(qrData))
            {
                ip = qrData.Trim();
                
#if UNITY_WEBGL && !UNITY_EDITOR
                port = 7778; // WebGL uses WebSocket port
#else
                port = Application.platform == RuntimePlatform.WebGLPlayer ? 7778 : 7777;
#endif
                Debug.Log($"[QRScanner] Using default port {port} for IP: {ip}");
            }
            else
            {
                statusText.text = "Invalid input!";
                Debug.LogError($"[QRScanner] Invalid format: {qrData}");
                ReturnToConnectionPanel();
                return;
            }

            if (string.IsNullOrEmpty(ip) || port <= 0 || port > 65535)
            {
                statusText.text = "Invalid IP or port!";
                Debug.LogError($"[QRScanner] Invalid - IP: {ip}, Port: {port}");
                ReturnToConnectionPanel();
                return;
            }

            statusText.text = $"Connecting to {ip}:{port}...";
            Debug.Log($"[QRScanner] Connecting - IP: {ip}, Port: {port}");

            if (isScanning)
            {
                StopScanning();
            }

            Invoke(() =>
            {
                if (MOBConnectionManager.Instance != null)
                {
                    MOBConnectionManager.Instance.ConnectToTV(ip, port);
                }
                else
                {
                    Debug.LogError("[QRScanner] ConnectionManager is null!");
                    statusText.text = "Connection Manager not found!";
                    ReturnToConnectionPanel();
                }
            }, 0.5f);
        }
        catch (Exception e)
        {
            statusText.text = "Failed to parse!";
            Debug.LogError($"[QRScanner] Parse error '{qrData}': {e.Message}\n{e.StackTrace}");
            ReturnToConnectionPanel();
        }
    }

    private void OnConnectionSuccess()
    {
        Debug.Log("[QRScanner] Connection successful!");
        statusText.text = "Connected!";
        
        // Hide camera display on successful connection
        if (cameraDisplayPanel != null)
        {
            cameraDisplayPanel.SetActive(false);
            Debug.Log("[QRScanner] Camera display panel deactivated (connected)");
        }
        
        if (scannerPanel != null)
            scannerPanel.SetActive(false);
        if (connectionPanel != null)
            connectionPanel.SetActive(false);
    }

    private void OnConnectionFailed()
    {
        Debug.LogWarning("[QRScanner] Connection failed!");
        statusText.text = "Connection failed! Check IP/Port.";
        
        // Hide camera display on connection failure
        if (cameraDisplayPanel != null)
        {
            cameraDisplayPanel.SetActive(false);
            Debug.Log("[QRScanner] Camera display panel deactivated (failed)");
        }
        
        ReturnToConnectionPanel();
    }

    private void ReturnToConnectionPanel()
    {
        Invoke(() =>
        {
            StopScanning();
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
        }, 2f);
    }

    public void StopScanning()
    {
        isScanning = false;
        CancelInvoke(nameof(ScanQRCode));

        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
            webcamTexture = null;
        }

        if (captureButton != null)
            captureButton.interactable = false;

        // Hide camera display when stopping
        if (cameraDisplayPanel != null)
        {
            cameraDisplayPanel.SetActive(false);
            Debug.Log("[QRScanner] Camera display panel deactivated (stopped)");
        }

        if (scannerPanel != null)
        {
            scannerPanel.SetActive(false);
        }
        
        Debug.Log("[QRScanner] Scanning stopped");
    }

    private void OnDestroy()
    {
        if (MOBConnectionManager.Instance != null)
        {
            MOBConnectionManager.Instance.OnTVConnected -= OnConnectionSuccess;
            MOBConnectionManager.Instance.OnTVFucked -= OnConnectionFailed;
        }

        isScanning = false;
        CancelInvoke(nameof(ScanQRCode));

        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
            webcamTexture = null;
        }
    }

    private void Invoke(Action action, float delay)
    {
        StartCoroutine(DelayedAction(action, delay));
    }

    private System.Collections.IEnumerator DelayedAction(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}