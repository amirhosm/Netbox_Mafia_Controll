using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NightPanel : MonoBehaviour
{
    [SerializeField] Transform listParent;
    [SerializeField] PlayerItem playerPrefab;
    [SerializeField] RTLTextMeshPro actTxt;
    [SerializeField] RTLTextMeshPro roleTxt;
    [SerializeField] RTLTextMeshPro timerTxt;
    [SerializeField] GameObject conformBtn;
    string myID, myAct, myTeam;
    string selectedID;
    GameManager gameManager;
    int nightNum;
    Dictionary<string, PlayerItem> AllPlayers = new Dictionary<string, PlayerItem>();
    bool confirmed;

    // Timer variables
    private Coroutine timerCoroutine;
    private bool timerComplete = false;

    public void Open(string[] datas, string MyID, GameManager gm)
    {
        gameObject.SetActive(true);
        gameManager = gm;
        myID = MyID;
        nightNum = int.Parse(datas[0].Split(':')[1]);

        if (listParent.childCount > 0) foreach (Transform t in listParent) Destroy(t.gameObject);

        for (int i = 1; i < datas.Length; i++)
        {
            PlayerItem playerItem = Instantiate(playerPrefab, listParent);
            playerItem.SetFromString(datas[i], this);

            if (playerItem.id == myID)
            {
                roleTxt.text = playerItem.role;
                actTxt.text = playerItem.GetActText();
                myAct = playerItem.roleAction;
                myTeam = playerItem.team;
                if (myAct != "Dr") playerItem.gameObject.SetActive(false);
            }
        }

        ShuffleChildren(listParent);

        if (nightNum == 1)
        {
            actTxt.text = (myTeam == "Black") ? "شب معارفه: مافیا رو ببین" : "شب معارفه: مافیا رو حدس بزن";
        }

        if (myTeam == "Black")
        {
            foreach (Transform item in listParent)
            {
                item.GetComponent<PlayerItem>().ShowToMafiaTeam(nightNum);
            }
        }

        if (myAct == "DieHard")
        {
            foreach (Transform item in listParent)
            {
                if (item.GetComponent<PlayerItem>().roleAction == "DieHard")
                    item.GetComponent<PlayerItem>().ShowDieHardButton();
            }
        }

        confirmed = false;
        timerComplete = false;

        // Hide button and start timer
        conformBtn.SetActive(false);

        // Start countdown timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        int timeRemaining = 10;
        timerTxt.gameObject.SetActive(true);

        // Update timer display
        while (timeRemaining > 0)
        {
            if (timerTxt != null)
            {
                timerTxt.text = timeRemaining.ToString();
            }

            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        // Timer complete - show 0
        if (timerTxt != null)
        {
            timerTxt.text = "0";
        }

        timerComplete = true;

        // Show button but keep it non-interactable until player selection
        conformBtn.SetActive(true);
        timerTxt.gameObject.SetActive(false);

        Button btnComponent = conformBtn.GetComponent<Button>();
        if (btnComponent != null)
        {
            btnComponent.interactable = false;
        }

        Debug.Log("[NightPanel] Timer complete. Button shown but disabled until player selection.");
    }

    public void SelectPlayer(string id)
    {
        if (confirmed) return;

        selectedID = id;

        foreach (Transform item in listParent)
        {
            item.GetComponent<PlayerItem>().ToggleSelected(item.GetComponent<PlayerItem>().id == id);
        }

        // Only show and enable button if timer is complete
        if (timerComplete)
        {
            conformBtn.SetActive(true);
            timerTxt.gameObject.SetActive(false);

            Button btnComponent = conformBtn.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.interactable = true;
            }

            Debug.Log("[NightPanel] Player selected. Confirm button enabled.");
        }
        else
        {
            Debug.Log("[NightPanel] Player selected but timer not complete yet.");
        }
    }

    public void OnBtn_Confirm()
    {
        if (myAct == "Spy")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selectedID)
                {
                    if (nightNum > 1)
                    {
                        t.GetComponent<PlayerItem>().RevealRole((t.GetComponent<PlayerItem>().roleAction == "Godfather" || t.GetComponent<PlayerItem>().team == "White") ? "شهروند" : "مافیا");
                        //gameManager.ShowMessage((t.GetComponent<PlayerItem>().roleAction == "Godfather" || t.GetComponent<PlayerItem>().team == "White") ? "شهروند" : "مافیا");
                    }
                }
            }
            gameManager.SendStringToTV("NightAct:Spy:" + selectedID);
        }
        else if (myAct == "Mafia")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().roleAction == "Godfather")
                {
                    Debug.Log("Send To Godfather >" + "NightMafiaKill:" + selectedID);
                    if (nightNum > 1)
                        gameManager.SendMessageTo(t.GetComponent<PlayerItem>().id, "NightMafiaKill:" + selectedID);
                }
            }
            gameManager.SendStringToTV("NightAct:Mafia:" + selectedID);
        }
        else if (myAct == "Lecter")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().roleAction == "Mafia")
                {
                    Debug.Log("Send To Mafia >" + "NightLecterSave:" + selectedID);
                    if (nightNum > 1)
                        gameManager.SendMessageTo(t.GetComponent<PlayerItem>().id, "NightLecterSave:" + selectedID);
                }
            }
            gameManager.SendStringToTV("NightAct:Mafia:" + selectedID);
        }
        else if (myAct == "Godfather")
        {
            //send to tv to kill by godfather
            gameManager.SendStringToTV("NightAct:Godfather:" + selectedID);
        }
        else if (myAct == "Citizen")
        {
            gameManager.SendStringToTV("NightAct:Citizen:" + selectedID);
        }
        else if (myAct == "Dr")
        {
            //send to tv to save
            gameManager.SendStringToTV("NightAct:Dr:" + selectedID);
        }
        else if (myAct == "Sniper")
        {
            //send to tv to kill by Sniper
            gameManager.SendStringToTV("NightAct:Sniper:" + selectedID);
        }

        foreach (Transform t in listParent)
        {
            t.GetComponent<Button>().interactable = false;
        }

        confirmed = true;

        conformBtn.SetActive(false);

        // Stop timer coroutine if still running
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    public void MafiaToGodfatherInNight(string selected)
    {
        if (listParent.childCount > 0)
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selected)
                {
                    t.GetComponent<PlayerItem>().SelectedToKill();
                }
            }
        }
    }

    public void LecterToMafiaInNight(string selected)
    {
        if (listParent.childCount > 0)
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selected)
                {
                    t.GetComponent<PlayerItem>().SelectedToSave();
                }
            }
        }
    }

    public static void ShuffleChildren(Transform parent)
    {
        int count = parent.childCount;
        if (count <= 1) return;

        // Collect children in a list
        List<Transform> children = new List<Transform>(count);
        for (int i = 0; i < count; i++)
            children.Add(parent.GetChild(i));

        // Fisher–Yates shuffle
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (children[i], children[j]) = (children[j], children[i]);
        }

        // Reapply sibling indices
        for (int i = 0; i < children.Count; i++)
            children[i].SetSiblingIndex(i);
    }
}
