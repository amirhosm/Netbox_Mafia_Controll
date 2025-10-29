using UnityEngine;
using UnityEngine.UI;
using ZXing;
using TMPro;
using UnityEngine.Android;

public class MOBQRScanner : MonoBehaviour
{
    [Header("UI References")]
    public RawImage cameraDisplay;
    public Button scanButton;
    public Button captureButton;
    public TextMeshProUGUI statusText;
    public GameObject scannerPanel;
    public GameObject connectionPanel;

    private WebCamTexture webcamTexture;
    private bool isScanning = false;

    private void Start()
    {
        if (scanButton != null)
            scanButton.onClick.AddListener(StartScanning);

        if (captureButton != null)
            captureButton.onClick.AddListener(CaptureAndScan);
    }

    public void StartScanning()
    {
        if (isScanning)
        {
            StopScanning();
            return;
        }

        // Hide connection panel, show scanner panel
        if (connectionPanel != null)
            connectionPanel.SetActive(false);

        scannerPanel.SetActive(true);
        statusText.text = "Initializing camera...";

        // Request camera permission on Android
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                statusText.text = "Requesting camera permission...";
                Invoke(nameof(CheckPermissionAndStart), 1f);
                return;
            }
        }

        InitializeCamera();
    }

    private void CheckPermissionAndStart()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            InitializeCamera();
        }
        else
        {
            statusText.text = "Camera permission denied!";
            scannerPanel.SetActive(false);
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
        }
    }

    private void InitializeCamera()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            statusText.text = "No camera found!";
            Debug.LogError("No camera devices found");
            return;
        }

        // Use back camera if available
        WebCamDevice[] devices = WebCamTexture.devices;
        string cameraName = devices[0].name;

        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
        cameraDisplay.texture = webcamTexture;

        // Fix rotation for mobile
        cameraDisplay.transform.localRotation = Quaternion.Euler(0, 0, -90);

        webcamTexture.Play();

        isScanning = true;
        statusText.text = "Point at QR code and tap CAPTURE";

        // Enable capture button
        if (captureButton != null)
            captureButton.interactable = true;

        // Auto-scan mode (continuous scanning)
        InvokeRepeating(nameof(ScanQRCode), 0.5f, 0.5f);
    }

    private void CaptureAndScan()
    {
        statusText.text = "Scanning...";
        ScanQRCode();
    }

    private void ScanQRCode()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return;

        try
        {
            IBarcodeReader barcodeReader = new BarcodeReader();

            // Get pixels and decode
            var result = barcodeReader.Decode(webcamTexture.GetPixels32(), webcamTexture.width, webcamTexture.height);

            if (result != null)
            {
                Debug.Log($"QR Code detected: {result.Text}");
                statusText.text = "QR Code found!";
                ProcessQRCode(result.Text);
                StopScanning();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error scanning QR code: {e.Message}");
        }
    }

    private void ProcessQRCode(string qrData)
    {
        // Expected format: "192.168.1.100:7777"
        string[] parts = qrData.Split(':');

        if (parts.Length == 2)
        {
            string ip = parts[0];
            int port;

            if (int.TryParse(parts[1], out port))
            {
                statusText.text = $"Connecting to {ip}:{port}...";

                // Connect after a short delay
                Invoke(() =>
                {
                    MOBConnectionManager.Instance.ConnectToTV(ip, port);
                }, 0.5f);
            }
            else
            {
                statusText.text = "Invalid QR code format!";
                Debug.LogError($"Invalid port in QR code: {parts[1]}");
                ReturnToConnectionPanel();
            }
        }
        else
        {
            statusText.text = "Invalid QR code!";
            Debug.LogError($"Invalid QR code format: {qrData}");
            ReturnToConnectionPanel();
        }
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
            webcamTexture = null;
        }

        if (captureButton != null)
            captureButton.interactable = false;

        // Check if object still exists before accessing
        if (scannerPanel != null)
        {
            scannerPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Only stop camera, don't touch UI
        isScanning = false;
        CancelInvoke(nameof(ScanQRCode));

        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }
    }

    // Helper method for delayed invoke
    private void Invoke(System.Action action, float delay)
    {
        StartCoroutine(DelayedAction(action, delay));
    }

    private System.Collections.IEnumerator DelayedAction(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
