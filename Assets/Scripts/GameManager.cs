using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] GameObject messagePanel;
    [SerializeField] GameObject keyboard;
    [SerializeField] GameObject blckScreen;
    [SerializeField] TMP_InputField keyboardInput;
    public RawImage avatarImg;
    [SerializeField] RTLTextMeshPro messageTxt;
    [SerializeField] private List<Sprite> Sprites;

    [Header("PANELs")]
    [SerializeField] GameObject lobbyPanel;
    [SerializeField] GameObject rolsPanel;
    [SerializeField] GameObject showRolePanel;
    [SerializeField] GameObject dayTalkPanel;
    [SerializeField] GameObject dayVotePanel;
    [SerializeField] GameObject diePanel;
    [SerializeField] GameObject winMafiaPanel;
    [SerializeField] GameObject winCitizenPanel;
    [SerializeField] GameObject kickedPanel;
    [SerializeField] NightPanel nightPanel;
    [SerializeField] GameObject transitionPanel;
    [SerializeField] GameObject AvatarsPanel;
    [SerializeField] GameObject MockConnectionPanel;
    [SerializeField] GameObject SessionFullPanel;
    [SerializeField] GameObject SessionRunningPanel;
    [Header("Show Role")]
    [SerializeField] RTLTextMeshPro showRoleTxt;
    [Header("Day Talk")]
    [SerializeField] RTLTextMeshPro dayTalkPlayerName;
    [SerializeField] Image dayAvatar;
    [Header("Day Vote")]
    [SerializeField] RTLTextMeshPro dayVotePlayerName;
    [SerializeField] Image voteAvatar;
    [SerializeField] GameObject dayVoteBtn;
    [SerializeField] GameObject dayVotePlayerBadge;
    [SerializeField] GameObject finalVoteAlert;
    [SerializeField] List<Transform> badgeSpawnPoints;
    [SerializeField] float badgeDisplayDuration = 3f;
    [Header("Lobby")]
    [SerializeField] Button lobbyReadyBtn;
    [SerializeField] Sprite readyBtnNormalSprite;
    [SerializeField] Sprite readyBtnDisabledSprite;

    private bool isPlayerReady = false;

    Dictionary<string, byte[]> AllAvatars = new Dictionary<string, byte[]>();
    Dictionary<string, string> playerNames = new Dictionary<string, string>();

    string underVoteID;
    string roleName, roleAct, roleTeam;
    private Animator transitionAnimator;
    private bool isTransitioning = false;

    private int AvatarID;

    // Struct to hold complete transition information
    private struct TransitionRequest
    {
        public System.Action panelSwitchAction;
        public GameObject targetPanel;

        public TransitionRequest(System.Action action, GameObject panel)
        {
            panelSwitchAction = action;
            targetPanel = panel;
        }
    }

    // Queue system for handling multiple rapid transitions
    private Queue<TransitionRequest> transitionQueue = new Queue<TransitionRequest>();

    // Track which panels have been shown for the first time in the current session
    private HashSet<GameObject> panelsShownFirstTime = new HashSet<GameObject>();

    // Track used spawn points to avoid duplicates
    private List<int> availableSpawnPointIndices = new List<int>();

    // Track the active transition coroutine
    private Coroutine activeTransitionCoroutine;

    private void Start()
    {
        //nameInput.onSelect.AddListener((t) =>
        //{
        //    keyboard.SetActive(true);
        //});

        // Get the Animator component from the transition panel
        if (transitionPanel != null)
        {
            transitionAnimator = transitionPanel.GetComponent<Animator>();
            if (transitionAnimator == null)
            {
                Debug.LogError("Transition panel does not have an Animator component!");
            }
        }
        else
        {
            Debug.LogError("Transition panel is not assigned!");
        }
    }

    private void Update()
    {
        // Check if screen is not in fullscreen mode
        //if (!Screen.fullScreen && !lobbyPanel.activeInHierarchy)
        //{
        //    if (rolsPanel.activeInHierarchy ||
        //        showRolePanel.activeInHierarchy ||
        //        dayTalkPanel.activeInHierarchy ||
        //        dayVotePanel.activeInHierarchy ||
        //        winMafiaPanel.activeInHierarchy ||
        //        winCitizenPanel.activeInHierarchy ||
        //        kickedPanel.activeInHierarchy ||
        //        nightPanel.gameObject.activeInHierarchy ||
        //        diePanel.activeInHierarchy)
        //    {
        //        // Detect any user interaction
        //        if (Input.touchCount > 0 ||           // Touch input
        //            Input.GetMouseButtonDown(0) ||     // Left mouse click
        //            Input.GetMouseButtonDown(1) ||     // Right mouse click
        //            Input.GetMouseButtonDown(2) ||     // Middle mouse click
        //            Input.anyKeyDown)                  // Any keyboard input
        //        {
        //            Screen.fullScreen = true;
        //            Debug.Log("Returning to fullscreen due to user interaction");
        //        }
        //    }            
        //}
    }

    // Main transition method that handles the smooth panel transitions
    private void TransitionToPanel(System.Action panelSwitchAction, GameObject targetPanel = null)
    {
        // Check if this is the first time showing this panel
        bool shouldTransition = targetPanel == null || !panelsShownFirstTime.Contains(targetPanel);

        if (shouldTransition)
        {
            //Close message panel if open
            messagePanel.SetActive(false);

            // Mark panel as shown and reset all other panels' first-time status
            if (targetPanel != null)
            {
                panelsShownFirstTime.Clear();
                panelsShownFirstTime.Add(targetPanel);
            }

            if (isTransitioning)
            {
                // If already transitioning, queue the complete request with all context
                Debug.Log($"[Transition] Currently transitioning, queueing request. Target: {(targetPanel != null ? targetPanel.name : "CLOSE-ONLY")}. Queue size: {transitionQueue.Count}");
                transitionQueue.Enqueue(new TransitionRequest(panelSwitchAction, targetPanel));
                return;
            }

            // Start the transition
            Debug.Log($"[Transition] Starting new transition. Target: {(targetPanel != null ? targetPanel.name : "CLOSE-ONLY")}");
            activeTransitionCoroutine = StartCoroutine(PerformTransition(panelSwitchAction, targetPanel));
        }
        else
        {
            // No transition, just execute the action immediately
            Debug.Log($"[Transition] Panel {targetPanel.name} already shown, executing immediately without transition");
            panelSwitchAction?.Invoke();
        }
    }

    private IEnumerator PerformTransition(System.Action panelSwitchAction, GameObject targetPanel = null)
    {
        isTransitioning = true;
        bool isCloseOnly = (targetPanel == null);

        Debug.Log($"[Transition] PerformTransition started. Type: {(isCloseOnly ? "CLOSE-ONLY" : "FULL")}. Target: {(targetPanel != null ? targetPanel.name : "none")}");

        // Show transition panel and play close animation
        transitionPanel.SetActive(true);
        transitionAnimator.Play("Transition_Close", 0, 0f); // Force start from beginning

        // Wait for close animation to complete
        float closeAnimLength = GetAnimationLength("Transition_Close");
        Debug.Log($"[Transition] Playing close animation ({closeAnimLength}s)");
        yield return new WaitForSeconds(closeAnimLength);

        // Execute the panel switch action
        panelSwitchAction?.Invoke();
        Debug.Log($"[Transition] Panel switch action executed");

        yield return new WaitForSeconds(0.1f); // Small buffer to ensure panel state is updated

        // Only play open animation if targetPanel is not null
        if (!isCloseOnly)
        {
            // Play open animation
            transitionAnimator.Play("Transition_Open", 0, 0f); // Force start from beginning

            // Wait for open animation to complete
            float openAnimLength = GetAnimationLength("Transition_Open");
            Debug.Log($"[Transition] Playing open animation ({openAnimLength}s)");
            yield return new WaitForSeconds(openAnimLength);

            // Hide transition panel only after opening a new panel
            transitionPanel.SetActive(false);
            Debug.Log($"[Transition] Transition panel hidden after open animation");
        }
        else
        {
            // For close-only: keep transition panel visible (covering screen)
            Debug.Log($"[Transition] CLOSE-ONLY complete. Transition panel remains visible covering screen.");
        }

        isTransitioning = false;
        Debug.Log($"[Transition] Transition complete. Queue size: {transitionQueue.Count}");

        // Process next transition in queue if any
        if (transitionQueue.Count > 0)
        {
            var nextRequest = transitionQueue.Dequeue();
            Debug.Log($"[Transition] Processing next queued transition. Target: {(nextRequest.targetPanel != null ? nextRequest.targetPanel.name : "CLOSE-ONLY")}. Remaining in queue: {transitionQueue.Count}");

            // Start the next transition with complete context preserved
            activeTransitionCoroutine = StartCoroutine(PerformTransition(nextRequest.panelSwitchAction, nextRequest.targetPanel));
        }
        else
        {
            activeTransitionCoroutine = null;
            Debug.Log($"[Transition] Queue empty. No more transitions pending.");
        }
    }

    // Helper method to get animation clip length
    private float GetAnimationLength(string animationName)
    {
        if (transitionAnimator == null) return 0.5f; // Default fallback

        AnimationClip[] clips = transitionAnimator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        Debug.LogWarning($"[Transition] Animation '{animationName}' not found, using default length");
        return 0.5f; // Default fallback if animation not found
    }

    public void SetImage(byte[] imageBytes)
    {
        // Convert byte array to Texture2D
        try
        {
            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(imageBytes); // Auto-resizes texture

            if (loaded)
            {
                avatarImg.texture = texture;
                Debug.Log($"[TVDebugDataDisplay] Image loaded: {texture.width}x{texture.height}");
            }
            else
            {
                Debug.LogError("[TVDebugDataDisplay] Failed to load image data!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TVDebugDataDisplay] Error loading image: {e.Message}");
        }
    }

    public void OnBtn_KetboardOk()
    {
        keyboard.SetActive(false);
    }

    public void OnLobbyReadyBtn()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);

        if (!string.IsNullOrEmpty(nameInput.text))
        {
            // ✨ NEW: Save username to PlayerPrefs
            //MOBConnectionManager.Instance.SaveUsername(nameInput.text);

            isPlayerReady = !isPlayerReady;

            if (isPlayerReady)
            {
                GetComponent<MOBGameSDK>().SendStringToTV("ready|" + nameInput.text + "|" + AvatarID);

                if (lobbyReadyBtn != null && readyBtnDisabledSprite != null)
                {
                    lobbyReadyBtn.image.sprite = readyBtnDisabledSprite;
                }
                //Screen.fullScreen = true;
            }
            else
            {
                // Player is not ready
                GetComponent<MOBGameSDK>().SendStringToTV("notready|" + nameInput.text + "|" + AvatarID);

                // Change button sprite back to normal
                if (lobbyReadyBtn != null && readyBtnNormalSprite != null)
                {
                    lobbyReadyBtn.image.sprite = readyBtnNormalSprite;
                }
            }
        }
        else
        {
            ShowMessage("نام را وارد کن");
        }
    }

    public void ShowMessage(string message)
    {
        messageTxt.text = message;
        messagePanel.SetActive(true);
    }

    // Updated methods with transition support
    public void GotoRolesPanel()
    {
        TransitionToPanel(() =>
        {
            lobbyPanel.SetActive(false);
            blckScreen.SetActive(true);
            rolsPanel.SetActive(true);
            if (lobbyReadyBtn != null && readyBtnNormalSprite != null)
            {
                lobbyReadyBtn.image.sprite = readyBtnNormalSprite;
            }
        }, rolsPanel);
    }

    public void GotoShowRolePanel(string RoleName, string RoleTeam, string RoleAct)
    {
        TransitionToPanel(() =>
        {
            rolsPanel.SetActive(false);
            showRolePanel.SetActive(true);

            roleName = RoleName;
            roleAct = RoleAct;
            roleTeam = RoleTeam;
            showRoleTxt.text = RoleName;
            if (lobbyReadyBtn != null && readyBtnNormalSprite != null)
            {
                lobbyReadyBtn.image.sprite = readyBtnNormalSprite;
            }

            showRolePanel.GetComponent<RoleShowPanel>().SetData(RoleName, RoleAct);
            Debug.Log(roleAct + ">" + roleTeam);
        }, showRolePanel);
    }

    public void SetRoleReconnect(string RoleName, string RoleTeam, string RoleAct)
    {

        roleName = RoleName;
        roleAct = RoleAct;
        roleTeam = RoleTeam;
        showRoleTxt.text = RoleName;

        showRolePanel.GetComponent<RoleShowPanel>().SetData(RoleName, RoleAct);
        Debug.Log(roleAct + ">" + roleTeam);
    }

    public void ShowDayTalk(string turnID, string turnName, string avatarID)
    {
        // Store player name for later use
        if (!playerNames.ContainsKey(turnID))
        {
            playerNames[turnID] = turnName;
        }

        TransitionToPanel(() =>
        {
            showRolePanel.SetActive(false);
            dayTalkPlayerName.text = turnName;
            dayTalkPanel.SetActive(true);
            dayTalkPanel.GetComponent<Animator>().Play("NewPlayerBeginTalk");
            dayAvatar.sprite = Sprites[int.Parse(avatarID)];
            if (lobbyReadyBtn != null && readyBtnNormalSprite != null)
            {
                lobbyReadyBtn.image.sprite = readyBtnNormalSprite;
            }
        }, dayTalkPanel);
    }


    bool hasVotedForToday;
    public void ShowDayVote(string turnID, string turnName, string avatarID, bool finalVote)
    {
        // Store player name for later use
        if (!playerNames.ContainsKey(turnID))
        {
            playerNames[turnID] = turnName;
        }

        // Reset available spawn points for new voting round
        ResetBadgeSpawnPoints();

        TransitionToPanel(() =>
        {
            dayTalkPanel.SetActive(false);
            underVoteID = turnID;
            dayVotePlayerName.text = turnName;
            voteAvatar.sprite = Sprites[int.Parse(avatarID)];
            dayVotePanel.SetActive(true);
            if (turnID == GetComponent<MOBGameSDK>().GetMyPlayerId())
            {
                dayVoteBtn.SetActive(false);
            }
            else
            {
                dayVoteBtn.SetActive(true);
            }
            if(finalVote && hasVotedForToday) dayVoteBtn.SetActive(false);
            finalVoteAlert.SetActive(finalVote);
        }, dayVotePanel);
    }

    public void EndDayVoting()
    {
        hasVotedForToday = false;

        TransitionToPanel(() =>
        {
            dayTalkPanel.SetActive(false);
            dayVotePanel.SetActive(false);
        }); // targetPanel is null - CLOSE-ONLY, keeps transition panel visible
    }

    public void OnBtn_DayVote()
    {
        hasVotedForToday = true;

        dayVoteBtn.SetActive(false);
        SendStringToTV("vote");

        // Notify all other players that this player has voted
        BroadcastVoteNotification();
    }

    // Broadcast vote notification to all other players
    private void BroadcastVoteNotification()
    {
        string myPlayerId = GetComponent<MOBGameSDK>().GetMyPlayerId();

        // Send a broadcast message through the TV to all other players
        // Format: "VOTE_NOTIFICATION:playerID"
        SendStringToTV($"broadcast_vote:{myPlayerId}");
    }

    // This method is called when receiving a vote notification from another player
    public void OnPlayerVoted(string voterPlayerId)
    {
        // Don't show badge for own vote
        string myPlayerId = GetComponent<MOBGameSDK>().GetMyPlayerId();
        if (voterPlayerId == myPlayerId)
        {
            Debug.Log("Ignoring own vote notification");
            return;
        }

        Debug.Log($"Player {voterPlayerId} has voted! Showing badge...");

        // Get the player name from the dictionary
        string voterName = playerNames.ContainsKey(voterPlayerId) ? playerNames[voterPlayerId] : "Unknown Player";

        // Show the vote badge with player name
        ShowVoteBadge(voterName);
    }

    private void ResetBadgeSpawnPoints()
    {
        availableSpawnPointIndices.Clear();

        if (badgeSpawnPoints != null && badgeSpawnPoints.Count > 0)
        {
            for (int i = 0; i < badgeSpawnPoints.Count; i++)
            {
                availableSpawnPointIndices.Add(i);
            }
            Debug.Log($"Reset badge spawn points. Available: {availableSpawnPointIndices.Count}");
        }
    }

    private void ShowVoteBadge(string playerName)
    {
        if (dayVotePlayerBadge == null)
        {
            Debug.LogError("dayVotePlayerBadge is not assigned!");
            return;
        }

        // Instantiate the badge
        GameObject badgeInstance;

        // Select random spawn point from available ones
        if (badgeSpawnPoints != null && badgeSpawnPoints.Count > 0 && availableSpawnPointIndices.Count > 0)
        {
            // If all spawn points are used, reset the available list
            if (availableSpawnPointIndices.Count == 0)
            {
                ResetBadgeSpawnPoints();
            }

            // Pick a random index from available indices
            int randomIndex = Random.Range(0, availableSpawnPointIndices.Count);
            int spawnPointIndex = availableSpawnPointIndices[randomIndex];

            // Remove this index from available list
            availableSpawnPointIndices.RemoveAt(randomIndex);

            Transform selectedSpawnPoint = badgeSpawnPoints[spawnPointIndex];
            badgeInstance = Instantiate(dayVotePlayerBadge, selectedSpawnPoint.position, Quaternion.identity, selectedSpawnPoint);

            Debug.Log($"Badge spawned at point {spawnPointIndex}. Remaining available: {availableSpawnPointIndices.Count}");
        }
        else
        {
            // Fallback: If no spawn points are set, instantiate as child of the dayVotePanel
            Debug.LogWarning("No badge spawn points assigned! Using dayVotePanel as parent.");
            badgeInstance = Instantiate(dayVotePlayerBadge, dayVotePanel.transform);
        }

        badgeInstance.SetActive(true);

        // Find TextMeshPro component in children and set the player name
        TextMeshProUGUI tmpComponent = badgeInstance.GetComponentInChildren<TextMeshProUGUI>();
        RTLTextMeshPro rtlTmpComponent = badgeInstance.GetComponentInChildren<RTLTextMeshPro>();

        if (rtlTmpComponent != null)
        {
            rtlTmpComponent.text = playerName;
            Debug.Log($"Set RTLTextMeshPro text to: {playerName}");
        }
        else if (tmpComponent != null)
        {
            tmpComponent.text = playerName;
            Debug.Log($"Set TextMeshProUGUI text to: {playerName}");
        }
        else
        {
            Debug.LogWarning("No TextMeshPro component found in badge children!");
        }

        // Destroy the badge after the specified duration
        StartCoroutine(DestroyBadgeAfterDelay(badgeInstance, badgeDisplayDuration));
    }

    private IEnumerator DestroyBadgeAfterDelay(GameObject badge, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (badge != null)
        {
            Destroy(badge);
            Debug.Log("Vote badge destroyed");
        }
    }

    public void GotoNight(string[] datas)
    {
        // Store player names from night data for vote notifications
        for (int i = 1; i < datas.Length; i++)
        {
            string[] playerData = datas[i].Split(':');
            if (playerData.Length >= 2)
            {
                string playerId = playerData[0];
                string playerName = playerData[1];
                if (!playerNames.ContainsKey(playerId))
                {
                    playerNames[playerId] = playerName;
                }
            }
        }

        TransitionToPanel(() =>
        {
            dayVotePanel.SetActive(false);
            dayTalkPanel.SetActive(false);
            nightPanel.Open(datas, GetComponent<MOBGameSDK>().GetMyPlayerId(), this);
        }, nightPanel.gameObject);
    }

    public void EndNight()
    {
        TransitionToPanel(() =>
        {
            nightPanel.gameObject.SetActive(false);
        }); // targetPanel is null - CLOSE-ONLY, keeps transition panel visible
    }

    public void SendMessageTo(string targetPlayerId, string message)
    {
        GetComponent<MOBGameSDK>().SendMyMessageTo(targetPlayerId, message);
    }

    public void MafiaToGodfatherInNight(string id)
    {
        nightPanel.MafiaToGodfatherInNight(id);
    }

    public void LecterToMafiaInNight(string id)
    {
        nightPanel.LecterToMafiaInNight(id);
    }

    public void SendStringToTV(string message)
    {
        GetComponent<MOBGameSDK>().SendStringToTV(message);
    }

    public void ShowDie()
    {
        TransitionToPanel(() =>
        {
            diePanel.SetActive(true);
        }, diePanel);
    }

    public void ShowWinMafia()
    {
        TransitionToPanel(() =>
        {
            winMafiaPanel.SetActive(true);
        }, winMafiaPanel);
    }

    public void ShowWinCitizen()
    {
        TransitionToPanel(() =>
        {
            winCitizenPanel.SetActive(true);
        }, winCitizenPanel);
    }

    public void ShowKicked()
    {
        TransitionToPanel(() =>
        {
            kickedPanel.SetActive(true);
        }, kickedPanel);
    }

    public void ShowSessionFull()
    {
        SessionFullPanel.SetActive(true);
    }

    public void ShowSessionRunning()
    {
        SessionRunningPanel.SetActive(true);
    }

    public void AddAvatar(string playerId, byte[] avatar)
    {
        AllAvatars.Add(playerId, avatar);
    }

    public void ResetAll()
    {
        Debug.Log("[Transition] ResetAll called - stopping all transitions");

        // Stop the active transition coroutine specifically
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }

        isTransitioning = false;
        transitionQueue.Clear();

        // Reset first-time panel tracking
        panelsShownFirstTime.Clear();

        // Clear player names dictionary
        playerNames.Clear();

        // Reset ready state
        isPlayerReady = false;

        // FIXED: Properly reset the button state
        if (lobbyReadyBtn != null)
        {
            // Deselect the button in EventSystem
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == lobbyReadyBtn.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // Reset interactable state to force visual refresh
            lobbyReadyBtn.interactable = false;
            lobbyReadyBtn.interactable = true;

            // Now set the sprite
            if (readyBtnNormalSprite != null)
            {
                lobbyReadyBtn.image.sprite = readyBtnNormalSprite;
            }

            Debug.Log("[ResetAll] Lobby ready button reset to normal state");
        }

        // Hide transition panel
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }

        // Reset all panels immediately (no transition needed for reset)
        lobbyPanel.SetActive(true);
        rolsPanel.SetActive(false);
        showRolePanel.SetActive(false);
        dayTalkPanel.SetActive(false);
        dayVotePanel.SetActive(false);
        diePanel.SetActive(false);
        winMafiaPanel.SetActive(false);
        winCitizenPanel.SetActive(false);
        kickedPanel.SetActive(false);
        nightPanel.gameObject.SetActive(false);
    }

    // Optional: Method to transition without closing current panels (for overlays)
    public void ShowOverlayPanel(GameObject panel)
    {
        TransitionToPanel(() =>
        {
            panel.SetActive(true);
        }, panel);
    }

    // Optional: Method for immediate panel switch without transition (for emergency cases)
    public void ImmediatePanelSwitch(System.Action panelSwitchAction)
    {
        Debug.Log("[Transition] ImmediatePanelSwitch called - forcing immediate transition");

        // Stop the active transition coroutine specifically
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }

        isTransitioning = false;
        transitionQueue.Clear();
        transitionPanel.SetActive(false);
        panelSwitchAction?.Invoke();
    }

    public void OnAvatarImageClicked(int avatarId)
    {
        AvatarID = avatarId;
        avatarImg.texture = Sprites[AvatarID].texture;
    }

    public void OnClickAvatarOpenPanel()
    {
        TransitionToPanel(() =>
        {
            AvatarsPanel.SetActive(true);
        }, AvatarsPanel);

    }

    public void OnClickAvatarClosePanel()
    {
        GetComponent<MOBGameSDK>().SendStringToTV("avatar|" + AvatarID);

        TransitionToPanel(() =>
        {
            AvatarsPanel.SetActive(false);
        }, lobbyPanel);
    }
}