# MOBApp (Mobile Controller) Unity Setup Guide

## Required Packages
1. **TextMeshPro** - Already in Unity
2. **ZXing.Net** - For QR Code scanning
   - Download from: https://github.com/micjahn/ZXing.Net
   - Place DLL in `Assets/Plugins/`

## Scene Hierarchy Setup

Create the following hierarchy in your Unity scene:

```
MOBAppScene
├── GameManager (Empty GameObject)
│   └── MOBConnectionManager (Script)
│
├── Canvas (Canvas - Screen Space Overlay)
│   ├── ConnectionPanel
│   │   ├── Background (Image - Full screen, semi-transparent)
│   │   ├── Logo (Image - Your game logo)
│   │   ├── ScanButton (Button)
│   │   │   └── Text (TextMeshProUGUI) - "Scan QR Code"
│   │   ├── ConnectionStatusText (TextMeshProUGUI)
│   │   └── PlayerIDText (TextMeshProUGUI)
│   │
│   ├── ScannerPanel (Initially disabled)
│   │   ├── CameraDisplay (RawImage - Full screen)
│   │   ├── ScanFrame (Image - QR scan frame overlay)
│   │   └── StatusText (TextMeshProUGUI) - "Point at QR code"
│   │
│   └── ControllerPanel (Initially disabled)
│       ├── Background (Image - Full screen)
│       │
│       ├── DPad (Empty GameObject - Left side)
│       │   ├── Position: (-400, -400)
│       │   ├── BtnUp (Button + Image)
│       │   ├── BtnDown (Button + Image)
│       │   ├── BtnLeft (Button + Image)
│       │   └── BtnRight (Button + Image)
│       │
│       ├── ActionButtons (Empty GameObject - Right side)
│       │   ├── Position: (400, -400)
│       │   ├── BtnA (Button + Image) - Red
│       │   ├── BtnB (Button + Image) - Yellow
│       │   ├── BtnX (Button + Image) - Blue
│       │   └── BtnY (Button + Image) - Green
│       │
│       ├── SystemButtons (Top)
│       │   ├── BtnStart (Button) - "START"
│       │   └── BtnPause (Button) - "PAUSE"
│       │
│       └── PlayerInfoPanel (Top Left)
│           ├── PlayerIDText (TextMeshProUGUI)
│           └── StatusText (TextMeshProUGUI)
│
└── MOBUIManager (Empty GameObject)
    ├── MOBQRScanner (Script)
    └── MOBControllerUI (Script)
```

## Detailed Layout Instructions

### Canvas Settings
- Render Mode: Screen Space - Overlay
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1080x1920 (Portrait)
- Match: 0.5 (Width and Height)

### Connection Panel (Full Screen)
- Anchor: Stretch/Stretch
- Margins: 0

**Background:**
- Color: Dark gradient or solid (#1a1a1a)
- Image: Optional background image

**Logo:**
- Position: Top center (0, -200)
- Size: 400x400

**ScanButton:**
- Position: Center (0, 0)
- Size: 600x150
- Background: Bright accent color (#00ff00)
- Text: "SCAN QR CODE" - Size 48, Bold

**ConnectionStatusText:**
- Position: Below button (0, -200)
- Size: 800x100
- Text: "Not Connected"
- Font Size: 36
- Alignment: Center

**PlayerIDText:**
- Position: Below status (0, -300)
- Size: 800x80
- Text: "Player: ---"
- Font Size: 32
- Alignment: Center

### Scanner Panel (Full Screen)
- Initially SetActive(false)

**CameraDisplay (RawImage):**
- Anchor: Stretch/Stretch
- Margins: 0

**ScanFrame (Image):**
- Position: Center
- Size: 600x600
- Sprite: Square frame outline
- Color: White with alpha

**StatusText:**
- Position: Bottom (0, 200)
- Size: 900x100
- Font Size: 40
- Color: White
- Shadow for visibility

### Controller Panel (Full Screen)
- Initially SetActive(false)
- Background: Dark (#1a1a1a)

### D-Pad Layout (Left Side)

Position all D-Pad buttons relative to center (-400, -500):

**Button Sizes:** 150x150 each

**BtnUp:**
- Position: (0, 100) relative to DPad center
- Sprite: Up arrow

**BtnDown:**
- Position: (0, -100)
- Sprite: Down arrow

**BtnLeft:**
- Position: (-100, 0)
- Sprite: Left arrow

**BtnRight:**
- Position: (100, 0)
- Sprite: Right arrow

**D-Pad Style:**
- Background: Dark gray (#333333)
- Border: 2px white
- Icon: White arrows
- Pressed state: Lighter gray (#555555)

### Action Buttons Layout (Right Side)

Position relative to center (400, -500):

**Diamond Layout:**

**BtnA (Bottom):**
- Position: (0, -100)
- Color: Red (#ff0000)
- Text: "A"

**BtnB (Right):**
- Position: (100, 0)
- Color: Yellow (#ffff00)
- Text: "B"

**BtnX (Left):**
- Position: (-100, 0)
- Color: Blue (#0000ff)
- Text: "X"

**BtnY (Top):**
- Position: (0, 100)
- Color: Green (#00ff00)
- Text: "Y"

**Action Button Style:**
- Size: 140x140 (slightly smaller circle)
- Border: 3px white
- Text: 60pt, Bold, White with shadow
- Pressed state: Darker shade + scale 0.9

### System Buttons (Top Center)

**BtnStart:**
- Position: (-150, -150)
- Size: 200x80
- Color: Gray (#666666)
- Text: "START" - 36pt

**BtnPause:**
- Position: (150, -150)
- Size: 200x80
- Color: Gray (#666666)
- Text: "PAUSE" - 36pt

### Player Info Panel (Top Left)

**Panel Background:**
- Position: Top Left (50, -50)
- Size: 350x120
- Background: Semi-transparent (#000000 with 70% alpha)
- Border: 2px accent color

**PlayerIDText:**
- Size: 340x50
- Position: Top of panel
- Text: "player-1"
- Font Size: 32
- Color: Accent color

**StatusText:**
- Size: 340x50
- Position: Bottom of panel
- Text: "Connected"
- Font Size: 24
- Color: Green (#00ff00)

## Script Assignments

### GameManager GameObject

1. **Add MOBConnectionManager script**
   - No settings needed (all automatic)

### MOBUIManager GameObject

1. **Add MOBQRScanner script:**
   - Camera Display: Drag CameraDisplay (RawImage)
   - Scan Button: Drag ScanButton
   - Status Text: Drag StatusText (from ScannerPanel)
   - Scanner Panel: Drag ScannerPanel

2. **Add MOBControllerUI script:**
   - Connection Panel: Drag ConnectionPanel
   - Controller Panel: Drag ControllerPanel
   - Player ID Text: Drag PlayerIDText (from ControllerPanel)
   - Connection Status Text: Drag ConnectionStatusText
   - Btn Up/Down/Left/Right: Drag respective buttons
   - Btn A/B/X/Y: Drag respective buttons
   - Btn Start/Pause: Drag respective buttons
   - Use Gyroscope: Check (true)
   - Mouse Sensitivity: 100
   - Screen Size: (1920, 1080) - Match your TV resolution

## Button Sprites/Icons

You can use simple shapes or download free game controller icons:

**D-Pad:**
- Use Triangle shapes rotated for arrows
- Or download arrow icons from: https://kenney.nl/assets (free game assets)

**Action Buttons:**
- Use Circle shapes with text overlay
- Or use button icons with letters

**System Buttons:**
- Use Rectangle shapes with rounded corners
- Text overlay

## Android Mobile Build Settings

1. **File > Build Settings**
   - Platform: Android
   - Texture Compression: ASTC

2. **Player Settings:**
   - Company Name: Your Company
   - Product Name: Your Game Controller
   - Package Name: com.yourcompany.mobilecontroller
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Highest installed
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64

3. **Other Settings:**
   - Auto Graphics API: Disabled
   - Graphics APIs: OpenGLES3, Vulkan
   - Write Permission: External (SD Card)
   - Internet Access: Required

4. **Android Manifest Permissions:**
   ```xml
   <uses-permission android:name="android.permission.INTERNET" />
   <uses-permission android:name="android.permission.CAMERA" />
   <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
   <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
   ```

5. **Orientation:**
   - Default Orientation: Portrait
   - Or set to Auto Rotation if you want landscape option

## Your Game Implementation

Create your game script inheriting from `MOBGameSDK`:

```csharp
public class MyMobileGame : MOBGameSDK
{
    protected override void OnTVConnected()
    {
        base.OnTVConnected();
        
        // Player connected to TV
        SendStringToTV("Hello from mobile!");
    }
    
    protected override void OnTVGotString(string message)
    {
        base.OnTVGotString(message);
        
        if (message == "GAME_START")
        {
            // Start game
        }
    }
    
    private void Update()
    {
        if (IsConnectedToTV())
        {
            // Your game logic
        }
    }
}
```

## Testing

### Test on Android Device:
1. Build and install MOBApp on Android phone
2. Ensure phone is on same WiFi as Android TV
3. Open app
4. Tap "SCAN QR CODE"
5. Grant camera permission
6. Point at TV's QR code
7. Should connect and show controller

### Test Controller Input:
1. Press buttons on mobile
2. Check TV app shows button presses in real-time
3. Wave phone to test air mouse cursor movement

## Color Scheme Recommendations

**Dark Theme (Recommended):**
- Background: #1a1a1a
- Panels: #2d2d2d
- Text: #ffffff
- Accent: #00ff00 or your brand color

**Button Colors:**
- A: #ff4444 (Red)
- B: #ffdd44 (Yellow)  
- X: #4444ff (Blue)
- Y: #44ff44 (Green)
- D-Pad: #666666 (Gray)
- System: #555555 (Dark Gray)

**Light Theme (Alternative):**
- Background: #f5f5f5
- Panels: #ffffff
- Text: #1a1a1a
- Accent: #007AFF

## Gyroscope Calibration

To add a calibration button:

```csharp
public void RecenterAirMouse()
{
    currentMousePosition = new Vector2(960, 540); // Center of screen
}
```

Add a "RECENTER" button in the controller panel that calls this function.

## Network Troubleshooting

**If connection fails:**

1. Check both devices on same WiFi
2. Check router doesn't have AP Isolation enabled
3. Try manual IP entry as fallback
4. Add manual connection option:

```csharp
public InputField ipInput;
public InputField portInput;

public void ManualConnect()
{
    string ip = ipInput.text;
    int port = int.Parse(portInput.text);
    MOBConnectionManager.Instance.ConnectToTV(ip, port);
}
```

## Performance Optimization

1. **Reduce Update Frequency:**
   - Send input every 2-3 frames instead of every frame
   - Use `Time.time` to throttle sends

2. **Gyro Smoothing:**
   ```csharp
   Vector3 smoothedGyro = Vector3.Lerp(lastGyro, Input.gyro.rotationRateUnbiased, 0.5f);
   ```

3. **Battery Saving:**
   - Lower screen brightness
   - Reduce gyro update rate when not actively used
   - Set `Application.targetFrameRate = 30;`

## Additional Features (Optional)

### Vibration Feedback:
```csharp
Handheld.Vibrate(); // When button pressed
```

### Touch Gestures:
- Add swipe detection for special moves
- Pinch to zoom
- Two-finger rotation

### Voice Chat:
- Use Unity Microphone class
- Stream audio to TV

## SDK Usage in Your Games

The SDK handles all the complexity. In your mobile game, just:

1. Inherit from `MOBGameSDK`
2. Override the events you need
3. Use helper functions to communicate
4. Focus on your game logic!

Example: Simple multiplayer game

```csharp
public class MultiplayerMobileGame : MOBGameSDK
{
    public Texture2D myAvatar;
    
    protected override void OnTVConnected()
    {
        base.OnTVConnected();
        // Send my avatar to TV
        SendTextureToTV(myAvatar);
    }
    
    protected override void GotNudeFrom(string playerId, string imageDataBase64)
    {
        base.GotNudeFrom(playerId, imageDataBase64);
        // Another player sent their avatar
        // Display it in your game
    }
}
```

## Next Steps

1. Build both TVApp and MOBApp
2. Test connection between them
3. Test all buttons and gyroscope
4. Implement your actual game logic
5. Test with multiple mobile devices

## Common Issues & Solutions

**Camera not working:**
- Check AndroidManifest has CAMERA permission
- Request permission at runtime
- Test on real device (not emulator)

**Gyroscope not responding:**
- Check `SystemInfo.supportsGyroscope`
- Enable in code: `Input.gyro.enabled = true`
- Some devices have poor gyro sensors

**Connection timeout:**
- Increase timeout in TCP connect
- Check firewall/router settings
- Try different port (default is 7777)

**Buttons not registering:**
- Check EventTrigger component is added
- Verify button colliders are enabled
- Check button is not behind another UI element

## Files You Need

**Scripts (5 files):**
1. MOBConnectionManager.cs
2. MOBQRScanner.cs
3. MOBControllerUI.cs
4. MOBGameSDK.cs
5. (Your game script inheriting from MOBGameSDK)

**Assets:**
- Button sprites/icons
- Background images
- Sound effects (optional)
- Haptic feedback (optional)

The MOBApp SDK is now complete and ready to use in all your mobile games! 🎮📱