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
    [SerializeField] GameObject dieHardPanel;
    string myID, myAct, myTeam;
    string selectedID;
    GameManager gameManager;
    int nightNum;
    Dictionary<string, PlayerItem> AllPlayers = new Dictionary<string, PlayerItem>();
    bool confirmed;
    bool dieHardDecision;
    int dieHardCount;
    bool noGodfather, noDrLecter;
    int mafiaCount;

    // Timer variables
    private Coroutine timerCoroutine;
    private bool timerComplete = false;

    public void Open(string[] datas, string MyID, GameManager gm)
    {
        gameObject.SetActive(true);
        gameManager = gm;
        myID = MyID;
        noGodfather = true;
        noDrLecter = true;
        mafiaCount = 0;
        nightNum = int.Parse(datas[0].Split(':')[1]);

        if (listParent.childCount > 0) foreach (Transform t in listParent) Destroy(t.gameObject);


        for (int i = 1; i < datas.Length; i++)
        {
            PlayerItem playerItem = Instantiate(playerPrefab, listParent);
            playerItem.SetFromString(datas[i], this);

            if (playerItem.roleAction == "Godfather") noGodfather = false;
            if (playerItem.roleAction == "DrLecter") noDrLecter = false;
            if (playerItem.roleAction == "Mafia") mafiaCount++;

            if (playerItem.id == myID)
            {
                roleTxt.text = playerItem.role;
                actTxt.text = playerItem.GetActText();
                myAct = playerItem.roleAction;
                myTeam = playerItem.team;
                playerItem.gameObject.SetActive(false);
                if (myAct == "Dr" || myAct == "DrLecter") playerItem.gameObject.SetActive(true);
            }
        }

        ShuffleChildren(listParent);

        if (nightNum == 1)
        {
            actTxt.text = (myTeam == "Black") ? "شب معارفه: مافیا رو ببین" : "شب معارفه: مافیا رو حدس بزن";
        }

        if (myAct == "Sniper" && nightNum > 1)
        {
            PlayerItem playerItem = Instantiate(playerPrefab, listParent);
            playerItem.SetFromString("NO:بدون شلیک:NO:NO:NO", this);
            playerItem.transform.SetAsFirstSibling();
        }

        if (myTeam == "Black")
        {
            foreach (Transform item in listParent)
            {
                item.GetComponent<PlayerItem>().ShowToMafiaTeam(nightNum);
            }

            if (nightNum > 1)
            {
                if (noGodfather)
                {
                    if (myAct == "DrLecter")
                    {
                        actTxt.text = "یک نفر رابکش";
                    }
                    else
                    {
                        if (noDrLecter && mafiaCount == 1)
                        {
                            actTxt.text = "یک نفر رابکش";
                        }
                    }
                }
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

        
        if (myAct == "DieHard")
        {
            dieHardPanel.SetActive(false);

            if (nightNum == 1)
            {
                timerCoroutine = StartCoroutine(CountdownTimer());
            }
            else
            {
                dieHardPanel.SetActive(true);
            }
        }
        else
        {
            timerCoroutine = StartCoroutine(CountdownTimer());
        }
    }

    private IEnumerator CountdownTimer()
    {
        int timeRemaining = Random.Range(4, 11);

        if (myTeam == "Black")
            timeRemaining = 0;
        
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

    public void OnBTN_DieHard(bool yes)
    {
        dieHardDecision = yes;

        if (yes)
        {
            if (dieHardCount < 2)
            {
                dieHardCount++;
            }
            else
            {
                dieHardDecision = false;
                gameManager.ShowMessage("فقط 2 استعلام داشتی");
            }
        }

        dieHardPanel.SetActive(false);
        OnBtn_Confirm();
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
        else if (myAct == "DrLecter")
        {
            //send to tv to save
            gameManager.SendStringToTV("NightAct:DrLecter:" + selectedID);
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
        else if (myAct == "DieHard")
        {
            gameManager.SendStringToTV("NightAct:DieHard:" + (dieHardDecision ? 1 : 0));
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
