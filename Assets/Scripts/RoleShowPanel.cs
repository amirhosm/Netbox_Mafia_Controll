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
    [SerializeField] private Sprite Mafia2Sprite;
    [SerializeField] private Sprite MafiaFemaleSprite;
    [SerializeField] private Sprite DoctorLecterSprite;
    [SerializeField] private Sprite DoctorSprite;
    [SerializeField] private Sprite SniperSprite;
    [SerializeField] private Sprite CitizenMaleSprite;
    [SerializeField] private Sprite CitizenFemaleSprite;

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
            int rnd = Random.Range(0, 100);
            if (rnd > 66)
                RoleImage.sprite = MafiaSprite;
            else if (rnd <= 66 && rnd > 33)
                RoleImage.sprite = Mafia2Sprite;
            else if (rnd <= 33)
                RoleImage.sprite = MafiaFemaleSprite;
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
            int rnd = Random.Range(0, 100);
            if (rnd > 50)
                RoleImage.sprite = CitizenMaleSprite;
            else if (rnd <= 50)
                RoleImage.sprite = CitizenFemaleSprite;
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
