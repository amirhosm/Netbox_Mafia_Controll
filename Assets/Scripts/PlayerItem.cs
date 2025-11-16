using RTLTMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] RTLTextMeshPro nameTxt;
    [SerializeField] RTLTextMeshPro roleTxt;
    [SerializeField] GameObject selectBtn;
    [SerializeField] GameObject mafiaKill1;
    [SerializeField] GameObject mafiaKill2;
    [HideInInspector] public bool isSelected;
    [HideInInspector] public bool isDead;
    [HideInInspector] public string id, playerName, role;
    [HideInInspector] public string team;
    [HideInInspector] public string roleAction;
    NightPanel manager;

    public void SetFromString(string data, NightPanel Manager)
    {
        manager = Manager;
        //id + ":" + playerName + ":" + role + ":" + team + ":" + roleAction
        string[] datas = data.Split(':');
        id = datas[0];
        playerName = datas[1];
        role = datas[2];
        team = datas[3];
        roleAction = datas[4];

        nameTxt.text = playerName;
    }

    public void ToggleSelected(bool selected)
    {
        GetComponent<Image>().color = selected ? Color.green : Color.white;
        isSelected = selected;
    }

    public void OnBtn_Select()
    {
        manager.SelectPlayer(id);
    }

    public string GetActText()
    {
        string act = "";
        if (roleAction == "Citizen") act = "الکی انتخاب کن!";
        else if (roleAction == "Dr") act = "یک نفر را نجات بده";
        else if (roleAction == "Spy") act = "نقش یک نفر را ببین";
        else if (roleAction == "Sniper") act = "به یک نفر شلیک کن";
        else if (roleAction == "Godfather") act = "یک نفر رابکش";
        else if (roleAction == "Mafia") act = "یک نفر را پیشنهاد بده";
        return act;
    }

    public void ShowToMafiaTeam()
    {
        if (team == "Black")
        {
            selectBtn.SetActive(false);
            GetComponent<Button>().interactable = false;
            roleTxt.text = role;
        }
    }

    public void SelectedToKill()
    {
        if (mafiaKill1.activeSelf) mafiaKill2.SetActive(true);
        else mafiaKill1.SetActive(true);
    }

    public string GetDataString()
    {
        return id + ":" + playerName + ":" + role + ":" + team + ":" + roleAction;
    }
    
}

public enum RoleTeam
{
    White,
    Black
}
public enum RoleAction
{
    Citizen,
    Dr,
    Spy,
    Sniper,
    Godfather,
    Mafia
}
