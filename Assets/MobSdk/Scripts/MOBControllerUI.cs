using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private HashSet<string> pressedButtons = new HashSet<string>();
    private MOBConnectionManager connectionManager;

    [Header("Gyroscope")]
    public bool useGyroscope = true;
    private bool gyroEnabled = false;

    [Header("Air Mouse Settings")]
    public float mouseSensitivity = 100f;
    public Vector2 screenSize = new Vector2(1920, 1080); // TV screen size
    private Vector2 currentMousePosition = new Vector2(960, 540); // Center

    float currentRoll = 0;
    Vector3 lastGyro = Vector3.zero;
    private float moveSpeed = 0;
    private float rollSpeed = 0;

    private void Start()
    {
        connectionManager = MOBConnectionManager.Instance;

        // Subscribe to connection events
        connectionManager.OnTVConnected += OnConnected;
        connectionManager.OnTVFucked += OnDisconnected;

        // Setup button listeners
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

        // Recenter button (regular click, not hold)
        if (btnRecenter != null)
            btnRecenter.onClick.AddListener(RecenterAirMouse);

        // Start with connection panel
        ShowConnectionPanel();

        // Initialize gyroscope
        InitializeGyroscope();
    }

    private void InitializeGyroscope()
    {
        if (SystemInfo.supportsGyroscope && useGyroscope)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 0.01f;
            gyroEnabled = true;
            Debug.Log("Gyroscope enabled");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported or disabled");
        }
    }

    private void Update()
    {
        if (connectionManager.isConnected)
        {
            // Send input state every frame
            //SendInputState();

            // Send gyro data
            //if (gyroEnabled)
            //{
            //    SendGyroData();
            //    UpdateAirMouse();
            //}
        }
    }

    private void SetupButton(Button button, string buttonName)
    {
        if (button == null) return;

        // Add EventTrigger for press and release
        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // Button press
        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnButtonPressed(buttonName); });
        eventTrigger.triggers.Add(pointerDown);

        // Button release
        var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnButtonReleased(buttonName); });
        eventTrigger.triggers.Add(pointerUp);
    }

    private void OnButtonPressed(string buttonName)
    {
        pressedButtons.Add(buttonName);
        Debug.Log($"Button pressed: {buttonName}");

        //if (buttonName == "START")
        //{
        //    GetComponent<MOBGameSDK>().SendStringToTV("ready");
        //}
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

    private void SendGyroData()
    {
        Vector3 gyro = Input.gyro.rotationRateUnbiased;
        Vector3 accel = Input.gyro.userAcceleration;
        connectionManager.SendGyroData(gyro, accel);
    }

    private void UpdateAirMouse()
    {
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
            Debug.Log($"AirMouse | Pos: ({currentMousePosition.x:F0}, {currentMousePosition.y:F0}) | Gyro: ({gyro.x:F2}, {gyro.y:F2}, {gyro.z:F2})");
        }
    }

    public void RecenterAirMouse()
    {
        // Reset to screen center
        currentMousePosition = screenSize / 2f;
        currentRoll = 0f;
        lastGyro = Vector3.zero;

        if (connectionManager.isConnected)
        {
            connectionManager.SendMousePosition(currentMousePosition);
            connectionManager.SendStringToTV($"ROLL:{currentRoll}");
        }

        Debug.Log("Air mouse recentered!");
    }

    private void OnConnected()
    {
        Debug.Log("Connected to TV!");
        playerIdText.text = $"Player: {connectionManager.myPlayerId}";
        connectionStatusText.text = "Connected!";

        // Reset air mouse to center
        RecenterAirMouse();

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
    }
}