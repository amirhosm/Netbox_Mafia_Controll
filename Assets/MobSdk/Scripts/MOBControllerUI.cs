using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MOBControllerUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject connectionPanel;
    public GameObject controllerPanel;

    [Header("Connection UI")]
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI connectionStatusText;

    [Header("D-Pad Buttons")]
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;

    [Header("Action Buttons")]
    public Button btnA;
    public Button btnB;
    public Button btnX;
    public Button btnY;

    [Header("System Buttons")]
    public Button btnStart;
    public Button btnPause;
    public Button btnRecenter;
    public Button btnToggleMode; // NEW: Mode toggle button
    public Button btnEnableGyro;

    [Header("Mode Display")]
    public TextMeshProUGUI modeText; // NEW: Shows current mode

    private HashSet<string> pressedButtons = new HashSet<string>();
    private MOBConnectionManager connectionManager;

    [Header("Air Mouse Settings")]
    public float mouseSensitivity = 150f;
    public Vector2 screenSize = new Vector2(1920, 1080);
    private Vector2 currentMousePosition;
    private Vector3 lastGyro = Vector3.zero;

    // NEW: Control mode
    public enum ControlMode
    {
        Gyro,
        Touch
    }

    private ControlMode currentMode = ControlMode.Gyro;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int InitDeviceMotion();
    
    [DllImport("__Internal")]
    private static extern int GetDeviceTilt(out float outX, out float outY);
    
    [DllImport("__Internal")]
    private static extern int HasDeviceMotionPermission();
    
    [DllImport("__Internal")]
    private static extern void StopDeviceMotion();
    
    private bool deviceMotionInitialized = false;
    private bool deviceMotionPermissionRequested = false;
#endif

    private void Start()
    {
        //Screen.SetResolution(720, 1280, true);

        connectionManager = MOBConnectionManager.Instance;

        connectionManager.OnTVConnected += OnConnected;
        connectionManager.OnTVFucked += OnDisconnected;

        SetupButton(btnUp, "UP");
        SetupButton(btnDown, "DOWN");
        SetupButton(btnLeft, "LEFT");
        SetupButton(btnRight, "RIGHT");
        SetupButton(btnA, "A");
        SetupButton(btnB, "B");
        SetupButton(btnX, "X");
        SetupButton(btnY, "Y");
        SetupButton(btnStart, "START");
        SetupButton(btnPause, "PAUSE");

        if (btnRecenter != null)
            btnRecenter.onClick.AddListener(RecenterAirMouse);

        // NEW: Setup mode toggle button
        if (btnToggleMode != null)
            btnToggleMode.onClick.AddListener(ToggleControlMode);

        ShowConnectionPanel();

        currentMousePosition = screenSize / 2f;
        
        // Set initial mode
        UpdateModeDisplay();

        if (btnEnableGyro != null)
            btnEnableGyro.onClick.AddListener(RequestGyroPermission);
    }

//    private void Update()
//    {
//        if (connectionManager.isConnected)
//        {
//            SendInputState();
            
//            // Only update mouse if in the appropriate mode
//            if (currentMode == ControlMode.Gyro)
//            {
//#if UNITY_WEBGL && !UNITY_EDITOR
//                UpdateWebGLGyroMouse();
//#else
//                if (Application.platform != RuntimePlatform.WebGLPlayer)
//                {
//                    UpdateAirMouse();
//                }
//                else
//                {
//                    UpdateWebGLGyroMouse();
//                }
//#endif
//            }
//            else if (currentMode == ControlMode.Touch)
//            {
//                UpdateTouchMouse();
//            }
//        }
//    }

    // NEW: Toggle between Gyro and Touch modes
    public void ToggleControlMode()
    {
        if (currentMode == ControlMode.Gyro)
        {
            currentMode = ControlMode.Touch;
            Debug.Log("[MOBControllerUI] Switched to TOUCH mode");
        }
        else
        {
            currentMode = ControlMode.Gyro;
            Debug.Log("[MOBControllerUI] Switched to GYRO mode");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            // Reinitialize gyro if needed
            if (!deviceMotionInitialized && !deviceMotionPermissionRequested)
            {
                deviceMotionPermissionRequested = true;
                int result = InitDeviceMotion();
                if (result == 1)
                {
                    deviceMotionInitialized = true;
                }
            }
#endif
        }
        
        UpdateModeDisplay();
    }

    // NEW: Update mode display text
    private void UpdateModeDisplay()
    {
        if (modeText != null)
        {
            string mode = currentMode == ControlMode.Gyro ? "GYRO" : "TOUCH";
            modeText.text = $"Mode: {mode}";
            modeText.color = currentMode == ControlMode.Gyro ? Color.cyan : Color.yellow;
        }
        
        // Update toggle button text if it has a TextMeshProUGUI child
        if (btnToggleMode != null)
        {
            var buttonText = btnToggleMode.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = currentMode == ControlMode.Gyro ? "Switch to Touch" : "Switch to Gyro";
            }
        }
    }

    private void SetupButton(Button button, string buttonName)
    {
        if (button == null) return;

        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnButtonPressed(buttonName); });
        eventTrigger.triggers.Add(pointerDown);

        var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnButtonReleased(buttonName); });
        eventTrigger.triggers.Add(pointerUp);
    }

    private void OnButtonPressed(string buttonName)
    {
        pressedButtons.Add(buttonName);
        Debug.Log($"Button pressed: {buttonName}");
    }

    private void OnButtonReleased(string buttonName)
    {
        pressedButtons.Remove(buttonName);
        Debug.Log($"Button released: {buttonName}");
    }

    private void SendInputState()
    {
        string inputState = string.Join(",", pressedButtons);
        connectionManager.SendInput(inputState);
    }

    // NATIVE MOBILE: Gyro mode
    private void UpdateAirMouse()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.LogWarning("Gyroscope not supported");
            return;
        }

        if (!Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 0.01f;
        }

        Vector3 gyro = Input.gyro.rotationRateUnbiased;
        float deadzone = 0.05f;
        if (Mathf.Abs(gyro.x) < deadzone) gyro.x = 0;
        if (Mathf.Abs(gyro.y) < deadzone) gyro.y = 0;
        if (Mathf.Abs(gyro.z) < deadzone) gyro.z = 0;

        float deltaX = -gyro.y * mouseSensitivity * Time.deltaTime;
        float deltaY = gyro.x * mouseSensitivity * Time.deltaTime;

        currentMousePosition.x += deltaX;
        currentMousePosition.y += deltaY;

        currentMousePosition.x = Mathf.Clamp(currentMousePosition.x, 0, screenSize.x);
        currentMousePosition.y = Mathf.Clamp(currentMousePosition.y, 0, screenSize.y);

        connectionManager.SendMousePosition(currentMousePosition);

        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[Gyro] Pos: ({currentMousePosition.x:F0}, {currentMousePosition.y:F0}) | Gyro: ({gyro.x:F2}, {gyro.y:F2}, {gyro.z:F2})");
        }
    }

    // WEBGL: Gyro mode using DeviceOrientation API
    // Replace UpdateWebGLGyroMouse() with this enhanced version:
    private void UpdateWebGLGyroMouse()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    // Initialize device motion if not done yet
    if (!deviceMotionInitialized && !deviceMotionPermissionRequested)
    {
        deviceMotionPermissionRequested = true;
        Debug.Log("[MOBControllerUI] Calling InitDeviceMotion...");
        int result = InitDeviceMotion();
        
        Debug.Log($"[MOBControllerUI] InitDeviceMotion result: {result}");
        
        if (result == 1)
        {
            deviceMotionInitialized = true;
            Debug.Log("[MOBControllerUI] ✓ Device motion initialized successfully");
        }
        else
        {
            Debug.LogError($"[MOBControllerUI] ✗ Device motion init failed with code: {result}");
        }
    }

    // Get tilt data if initialized
    if (deviceMotionInitialized || HasDeviceMotionPermission() == 1)
    {
        if (!deviceMotionInitialized)
        {
            Debug.Log("[MOBControllerUI] Permission granted, marking as initialized");
            deviceMotionInitialized = true;
        }
        
        float tiltX = 0f;
        float tiltY = 0f;
        
        int status = GetDeviceTilt(out tiltX, out tiltY);
        
        if (status == 0)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("[MOBControllerUI] GetDeviceTilt returned 0 (not listening)");
            }
            return;
        }
        
        // Log raw values occasionally
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[MOBControllerUI] Raw tilt: X={tiltX:F3}, Y={tiltY:F3}");
        }
        
        // Apply deadzone
        float deadzone = 0.5f;
        if (Mathf.Abs(tiltX) < deadzone) tiltX = 0;
        if (Mathf.Abs(tiltY) < deadzone) tiltY = 0;

        // Calculate delta movement
        float deltaX = tiltX * mouseSensitivity * Time.deltaTime;
        float deltaY = tiltY * mouseSensitivity * Time.deltaTime;

        // Only update if there's actual movement
        if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
        {
            // Update position
            currentMousePosition.x += deltaX;
            currentMousePosition.y += deltaY;

            // Clamp to screen bounds
            currentMousePosition.x = Mathf.Clamp(currentMousePosition.x, 0, screenSize.x);
            currentMousePosition.y = Mathf.Clamp(currentMousePosition.y, 0, screenSize.y);

            // Send to TV
            connectionManager.SendMousePosition(currentMousePosition);

            // Debug log
            Debug.Log($"[WebGL Gyro] ✓ Pos: ({currentMousePosition.x:F0}, {currentMousePosition.y:F0}) | Tilt: ({tiltX:F2}, {tiltY:F2}) | Delta: ({deltaX:F2}, {deltaY:F2})");
        }
        else if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[WebGL Gyro] No movement - tilt below threshold");
        }
    }
    else
    {
        if (Time.frameCount % 120 == 0)
        {
            Debug.LogWarning("[MOBControllerUI] Device motion not initialized or no permission");
        }
    }
#endif
    }

    // NEW: Touch/Mouse mode with absolute positioning (accounting for 90° rotation)
    private void UpdateTouchMouse()
    {
        bool hasInput = false;
        Vector2 touchPosition = Vector2.zero;

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Only track if finger is touching (not lifted)
            if (touch.phase == TouchPhase.Began || 
                touch.phase == TouchPhase.Moved || 
                touch.phase == TouchPhase.Stationary)
            {
                touchPosition = touch.position;
                hasInput = true;
            }
        }
        // Fallback to mouse for desktop testing
        else if (Input.GetMouseButton(0))
        {
            touchPosition = Input.mousePosition;
            hasInput = true;
        }

        if (hasInput)
        {
            // Convert touch position to TV screen coordinates
            Vector2 tvPosition = MapTouchToTVScreen(touchPosition);
            
            // Update current position
            currentMousePosition = tvPosition;

            // Clamp to screen bounds (safety check)
            currentMousePosition.x = Mathf.Clamp(currentMousePosition.x, 0, screenSize.x);
            currentMousePosition.y = Mathf.Clamp(currentMousePosition.y, 0, screenSize.y);

            // Send to TV
            connectionManager.SendMousePosition(currentMousePosition);

            // Debug log occasionally
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Touch] Phone: ({touchPosition.x:F0}, {touchPosition.y:F0}) → TV: ({currentMousePosition.x:F0}, {currentMousePosition.y:F0})");
            }
        }
        // When finger is lifted, don't send position to avoid jumping
    }

    // FIXED: Map touch coordinates from phone screen to TV screen
    // Phone is held landscape but browser is portrait: 1080×1920
    // TV is landscape: 1920×1080
    // Solution: Rotate phone coordinates 90° clockwise (FIXED INVERSION)
    private Vector2 MapTouchToTVScreen(Vector2 touchPosition)
    {
        // Get phone screen dimensions (portrait mode)
        float phoneWidth = Screen.width;   // e.g., 1080
        float phoneHeight = Screen.height; // e.g., 1920

        // TV screen dimensions (landscape)
        float tvWidth = screenSize.x;  // 1920
        float tvHeight = screenSize.y; // 1080

        // Normalize phone coordinates (0-1 range)
        float normalizedPhoneX = touchPosition.x / phoneWidth;   // 0-1
        float normalizedPhoneY = touchPosition.y / phoneHeight;  // 0-1

        // FIXED: Correct 90° clockwise rotation
        // When phone is held horizontally (landscape):
        // - Moving finger UP (increasing Y) should move cursor LEFT (decreasing X)
        // - Moving finger RIGHT (increasing X) should move cursor UP (decreasing Y)

        float tvX = (1f - normalizedPhoneY) * tvWidth;    // Phone Y → TV X (inverted)
        float tvY = normalizedPhoneX * tvHeight;          // Phone X → TV Y (direct)

        return new Vector2(tvX, tvY);
    }

    public void RecenterAirMouse()
    {
        currentMousePosition = screenSize / 2f;
        lastGyro = Vector3.zero;
        
        if (connectionManager.isConnected)
        {
            connectionManager.SendMousePosition(currentMousePosition);
        }
        
        Debug.Log("Air mouse recentered!");
    }

    // NEW: Request gyro permission (for iOS/Android browsers)
    public void RequestGyroPermission()
    {
        Debug.Log("[MOBControllerUI] ===== RequestGyroPermission button clicked =====");

#if UNITY_WEBGL && !UNITY_EDITOR
    try
    {
        Debug.Log("[MOBControllerUI] Resetting permission flags...");
        deviceMotionPermissionRequested = false;
        deviceMotionInitialized = false;
        
        Debug.Log("[MOBControllerUI] Calling InitDeviceMotion from user interaction...");
        int result = InitDeviceMotion();
        Debug.Log($"[MOBControllerUI] InitDeviceMotion returned: {result}");
        
        if (result == 1 || result >= 0)
        {
            Debug.Log("[MOBControllerUI] ✓ Init successful, setting flags...");
            deviceMotionInitialized = true;
            deviceMotionPermissionRequested = true;
            currentMode = ControlMode.Gyro;
            UpdateModeDisplay();
            
            Debug.Log("[MOBControllerUI] ✓ Gyro permission granted!");
            
            // Show success message
            if (modeText != null)
            {
                modeText.text = "GYRO ENABLED!";
                modeText.color = Color.green;
            }
            
            // Hide the enable button if it exists
            if (btnEnableGyro != null)
            {
                btnEnableGyro.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"[MOBControllerUI] ✗ Init failed with code: {result}");
            
            if (modeText != null)
            {
                modeText.text = "GYRO DENIED";
                modeText.color = Color.red;
            }
        }
        
        // Check permission status
        int permStatus = HasDeviceMotionPermission();
        Debug.Log($"[MOBControllerUI] Permission status after init: {permStatus}");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[MOBControllerUI] Exception in RequestGyroPermission: {e.Message}\n{e.StackTrace}");
    }
#else
        Debug.Log("[MOBControllerUI] RequestGyroPermission only works in WebGL builds");
#endif
    }

    private void OnConnected()
    {
        Debug.Log("Connected to TV!");
        playerIdText.text = $"Player: {connectionManager.myPlayerId}";
        connectionStatusText.text = "Connected!";
        RecenterAirMouse();
        UpdateModeDisplay();
        Invoke(nameof(ShowControllerPanel), 1f);
    }

    private void OnDisconnected()
    {
        Debug.Log("Disconnected from TV!");
        connectionStatusText.text = "Disconnected!";
        ShowConnectionPanel();
    }

    private void ShowConnectionPanel()
    {
        connectionPanel.SetActive(true);
        controllerPanel.SetActive(false);
    }

    private void ShowControllerPanel()
    {
        connectionPanel.SetActive(false);
        controllerPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (connectionManager != null)
        {
            connectionManager.OnTVConnected -= OnConnected;
            connectionManager.OnTVFucked -= OnDisconnected;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (deviceMotionInitialized)
        {
            StopDeviceMotion();
        }
#endif
    }
}