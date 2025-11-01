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
    [SerializeField] NightPanel nightPanel;
    [Header("Show Role")]
    [SerializeField] RTLTextMeshPro showRoleTxt;
    [Header("Day Talk")]
    [SerializeField] RTLTextMeshPro dayTalkPlayerName;
    [Header("Day Vote")]
    [SerializeField] RTLTextMeshPro dayVotePlayerName;
    [SerializeField] GameObject dayVoteBtn;
    string underVoteID;
    string roleName, roleAct, roleTeam;

    private void Start()
    {
        nameInput.onSelect.AddListener((t) =>
        {
            keyboard.SetActive(true);
        });
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

    public void GotoRolesPanel()
    {
        lobbyPanel.SetActive(false);
        rolsPanel.SetActive(true);
    }

    public void GotoShowRolePanel(string RoleName, string RoleAct, string RoleTeam)
    {
        rolsPanel.SetActive(false);
        showRolePanel.SetActive(true);

        roleName = RoleName;
        roleAct = RoleAct;
        roleTeam = RoleTeam;
        showRoleTxt.text = RoleName;

        Debug.Log(roleAct + ">" + roleTeam);
    }

    public void ShowDayTalk(string turnID, string turnName)
    {
        showRolePanel.SetActive(false);

        dayTalkPlayerName.text = turnName;
        dayTalkPanel.SetActive(true);
    }

    public void ShowDayVote(string turnID, string turnName)
    {
        dayTalkPanel.SetActive(false);

        underVoteID = turnID;
        dayVotePlayerName.text = turnName;
        dayVotePanel.SetActive(true);
        dayVoteBtn.SetActive(true);
    }

    public void EndDayVoting()
    {
        dayVotePanel.SetActive(false);
    }

    public void OnBtn_DayVote()
    {
        dayVoteBtn.SetActive(false);
        SendStringToTV("vote");
    }

    public void GotoNight(string[] datas)
    {
        nightPanel.Open(datas, GetComponent<MOBGameSDK>().GetMyPlayerId(),this);
    }

    public void EndNight()
    {
        nightPanel.gameObject.SetActive(false);
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
        diePanel.SetActive(true);
    }
    public void ResetAll()
    {
        lobbyPanel.SetActive(false);
        rolsPanel.SetActive(false);
        showRolePanel.SetActive(false);
        dayTalkPanel.SetActive(false);
        dayVotePanel.SetActive(false);
        diePanel.SetActive(false);
        nightPanel.gameObject.SetActive(false);
    }

}
