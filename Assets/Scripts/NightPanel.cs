using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightPanel : MonoBehaviour
{
    [SerializeField] Transform listParent;
    [SerializeField] PlayerItem playerPrefab;
    [SerializeField] RTLTextMeshPro actTxt;
    [SerializeField] RTLTextMeshPro roleTxt;
    [SerializeField] GameObject conformBtn;
    string myID, myAct, myTeam;
    string selectedID;
    GameManager gameManager;
    int nightNum;
    Dictionary<string, PlayerItem> AllPlayers = new Dictionary<string, PlayerItem>();

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
                playerItem.gameObject.SetActive(false);
            }
        }

        ShuffleChildren(listParent);

        if (nightNum == 1)
        {
            actTxt.text = (myTeam == "Black") ? "شب معارفه: الکی انتخاب کن" : "شب معارفه: مافیا رو حدس بزن";
        }

        if (myTeam == "Black")
        {
            foreach (Transform item in listParent)
            {
                item.GetComponent<PlayerItem>().ShowToMafiaTeam();
            }
        }

        conformBtn.SetActive(false);
    }

    public void SelectPlayer(string id)
    {
        selectedID = id;
        foreach (Transform item in listParent)
        {
            item.GetComponent<PlayerItem>().ToggleSelected(item.GetComponent<PlayerItem>().id == id);
        }
        conformBtn.SetActive(true);
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
                        gameManager.ShowMessage((t.GetComponent<PlayerItem>().roleAction == "Godfather" || t.GetComponent<PlayerItem>().team == "White") ? "شهروند" : "مافیا");
                }
                Destroy(t.gameObject);
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
                Destroy(t.gameObject);
            }
            gameManager.SendStringToTV("NightAct:Mafia:" + selectedID);
        }
        else if (myAct == "Godfather")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selectedID)
                {
                    //send to tv to kill by godfather
                    gameManager.SendStringToTV("NightAct:Godfather:" + selectedID);
                }
                Destroy(t.gameObject);
            }
        }
        else if (myAct == "Citizen")
        {
            foreach (Transform t in listParent)
            {
                Destroy(t.gameObject);
            }
            gameManager.SendStringToTV("NightAct:Citizen:" + selectedID);
        }
        else if (myAct == "Dr")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selectedID)
                {
                    //send to tv to save
                    gameManager.SendStringToTV("NightAct:Dr:" + selectedID);
                }
                Destroy(t.gameObject);
            }
        }
        else if (myAct == "Sniper")
        {
            foreach (Transform t in listParent)
            {
                if (t.GetComponent<PlayerItem>().id == selectedID)
                {
                    //send to tv to kill by Sniper
                    gameManager.SendStringToTV("NightAct:Sniper:" + selectedID);
                }
                Destroy(t.gameObject);
            }
        }

        conformBtn.SetActive(false);
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
