using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] GameObject messagePanel;
    [SerializeField] GameObject keyboard;
    [SerializeField] TMP_InputField keyboardInput;
    public RawImage avatarImg;
    [SerializeField] RTLTextMeshPro messageTxt;
    [Header("PANELs")]
    [SerializeField] GameObject lobbyPanel;
    [SerializeField] GameObject rolsPanel;
    [SerializeField] GameObject showRolePanel;
    [SerializeField] GameObject dayTalkPanel;
    [SerializeField] GameObject dayVotePanel;
    [SerializeField] GameObject diePanel;
    [SerializeField] GameObject winMafiaPanel;
    [SerializeField] GameObject winCitizenPanel;
    [SerializeField] NightPanel nightPanel;
    [SerializeField] GameObject transitionPanel;
    [Header("Show Role")]
    [SerializeField] RTLTextMeshPro showRoleTxt;
    [Header("Day Talk")]
    [SerializeField] RTLTextMeshPro dayTalkPlayerName;
    [Header("Day Vote")]
    [SerializeField] RTLTextMeshPro dayVotePlayerName;
    [SerializeField] GameObject dayVoteBtn;
    
    string underVoteID;
    string roleName, roleAct, roleTeam;
    private Animator transitionAnimator;
    private bool isTransitioning = false;
    
    // Queue system for handling multiple rapid transitions
    private Queue<System.Action> transitionQueue = new Queue<System.Action>();

    private void Start()
    {
        nameInput.onSelect.AddListener((t) =>
        {
            keyboard.SetActive(true);
        });
        
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

    // Main transition method that handles the smooth panel transitions
    private void TransitionToPanel(System.Action panelSwitchAction)
    {
        if (isTransitioning)
        {
            // If already transitioning, queue the action
            transitionQueue.Enqueue(panelSwitchAction);
            return;
        }
        
        StartCoroutine(PerformTransition(panelSwitchAction));
    }
    
    private IEnumerator PerformTransition(System.Action panelSwitchAction)
    {
        isTransitioning = true;
        
        // Show transition panel and play close animation
        transitionPanel.SetActive(true);
        transitionAnimator.Play("Transition_Close");
        
        // Wait for close animation to complete
        yield return new WaitForSeconds(GetAnimationLength("Transition_Close"));
        
        // Execute the panel switch action
        panelSwitchAction?.Invoke();
        
        // Play open animation
        transitionAnimator.Play("Transition_Open");
        
        // Wait for open animation to complete
        yield return new WaitForSeconds(GetAnimationLength("Transition_Open"));
        
        // Hide transition panel
        transitionPanel.SetActive(false);
        
        isTransitioning = false;
        
        // Process next transition in queue if any
        if (transitionQueue.Count > 0)
        {
            var nextAction = transitionQueue.Dequeue();
            StartCoroutine(PerformTransition(nextAction));
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
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            GetComponent<MOBGameSDK>().SendStringToTV("ready|" + nameInput.text + "|a");
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
            rolsPanel.SetActive(true);
        });
    }

    public void GotoShowRolePanel(string RoleName, string RoleAct, string RoleTeam)
    {
        TransitionToPanel(() =>
        {
            rolsPanel.SetActive(false);
            showRolePanel.SetActive(true);

            roleName = RoleName;
            roleAct = RoleAct;
            roleTeam = RoleTeam;
            showRoleTxt.text = RoleName;

            Debug.Log(roleAct + ">" + roleTeam);
        });
    }

    public void ShowDayTalk(string turnID, string turnName)
    {
        TransitionToPanel(() =>
        {
            showRolePanel.SetActive(false);
            dayTalkPlayerName.text = turnName;
            dayTalkPanel.SetActive(true);
        });
    }

    public void ShowDayVote(string turnID, string turnName)
    {
        TransitionToPanel(() =>
        {
            dayTalkPanel.SetActive(false);
            underVoteID = turnID;
            dayVotePlayerName.text = turnName;
            dayVotePanel.SetActive(true);
            dayVoteBtn.SetActive(true);
        });
    }

    public void EndDayVoting()
    {
        TransitionToPanel(() =>
        {
            dayVotePanel.SetActive(false);
        });
    }

    public void OnBtn_DayVote()
    {
        dayVoteBtn.SetActive(false);
        SendStringToTV("vote");
    }

    public void GotoNight(string[] datas)
    {
        TransitionToPanel(() =>
        {
            nightPanel.Open(datas, GetComponent<MOBGameSDK>().GetMyPlayerId(), this);
        });
    }

    public void EndNight()
    {
        TransitionToPanel(() =>
        {
            nightPanel.gameObject.SetActive(false);
        });
    }

    public void SendMessageTo(string targetPlayerId, string message)
    {
        GetComponent<MOBGameSDK>().SendMyMessageTo(targetPlayerId, message);
    }

    public void MafiaToGodfatherInNight(string id)
    {
        nightPanel.MafiaToGodfatherInNight(id);
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
        });
    }

    public void ShowWinMafia()
    {
        TransitionToPanel(() =>
        {
            winMafiaPanel.SetActive(true);
        });
    }

    public void ShowWinCitizen()
    {
        TransitionToPanel(() =>
        {
            winCitizenPanel.SetActive(true);
        });
    }

    public void ResetAll()
    {
        // Stop any ongoing transitions and clear queue
        StopAllCoroutines();
        isTransitioning = false;
        transitionQueue.Clear();
        
        // Hide transition panel
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }
        
        // Reset all panels immediately (no transition needed for reset)
        lobbyPanel.SetActive(false);
        rolsPanel.SetActive(false);
        showRolePanel.SetActive(false);
        dayTalkPanel.SetActive(false);
        dayVotePanel.SetActive(false);
        diePanel.SetActive(false);
        winMafiaPanel.SetActive(false);
        winCitizenPanel.SetActive(false);
        nightPanel.gameObject.SetActive(false);
    }

    // Optional: Method to transition without closing current panels (for overlays)
    public void ShowOverlayPanel(GameObject panel)
    {
        TransitionToPanel(() =>
        {
            panel.SetActive(true);
        });
    }

    // Optional: Method for immediate panel switch without transition (for emergency cases)
    public void ImmediatePanelSwitch(System.Action panelSwitchAction)
    {
        StopAllCoroutines();
        isTransitioning = false;
        transitionQueue.Clear();
        transitionPanel.SetActive(false);
        panelSwitchAction?.Invoke();
    }
}
