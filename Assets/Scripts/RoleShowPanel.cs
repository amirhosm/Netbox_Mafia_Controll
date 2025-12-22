using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoleShowPanel : MonoBehaviour
{
    [SerializeField] private Image RoleImage;
    [SerializeField] private TMP_Text RoleNameText;

    [SerializeField] private Sprite GodfatherSprite;
    [SerializeField] private Sprite DetectiveSprite;
    [SerializeField] private Sprite MafiaSprite;
    [SerializeField] private Sprite DoctorLecterSprite;
    [SerializeField] private Sprite DoctorSprite;
    [SerializeField] private Sprite SniperSprite;
    [SerializeField] private Sprite CitizenSprite;
    [SerializeField] private Sprite DieHardSprite;

    bool RoleRevealed = false;
    bool RoleIsRevealable = true;

    private void OnEnable()
    {
        if (RoleRevealed)
        {
            GetComponent<Animator>().Play("RoleShow_Hide");
            RoleRevealed = false;
        }
    }

    public void SetData(string roleName, string roelAct)
    {
        RoleNameText.text = roleName;

        if (roelAct == "Godfather")
        {
            RoleImage.sprite = GodfatherSprite;
        }
        else if (roelAct == "Spy")
        {
            RoleImage.sprite = DetectiveSprite;
        }
        else if (roelAct == "Mafia")
        {
            RoleImage.sprite = MafiaSprite;
        }
        else if (roelAct == "DrLecter")
        {
            RoleImage.sprite = DoctorLecterSprite;
        }
        else if (roelAct == "Dr")
        {
            RoleImage.sprite = DoctorSprite;
        }
        else if (roelAct == "Sniper")
        {
            RoleImage.sprite = SniperSprite;
        }
        else if (roelAct == "Citizen")
        {
            RoleImage.sprite = CitizenSprite;
        }
        else if (roelAct == "DieHard")
        {
            RoleImage.sprite = DieHardSprite;
        }
    }


    public void OnRoleClicked()
    {
        Debug.Log("Role clicked");

        if (!RoleRevealed && RoleIsRevealable)
        {
            Debug.Log("Role Revealing");
            GetComponent<Animator>().Play("RoleShow_Reveal");
            RoleRevealed = true;
            RoleIsRevealable = false;
        }
        else if (RoleRevealed && RoleIsRevealable)
        {
            Debug.Log("Role Hiding");
            GetComponent<Animator>().Play("RoleShow_Hide");
            RoleRevealed = false;
            RoleIsRevealable = false;
        }

        FindFirstObjectByType<MOBGameSDK>().SendStringToTV("SeenRole");
    }

    public void AnimatorCallback()
    {
        RoleIsRevealable = true;
    }
}
