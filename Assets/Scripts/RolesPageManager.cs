using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class RoleItem
{
    public Sprite roleImage;
    public string title;
    public string description;
    
    public RoleItem(Sprite image, string roleTitle, string roleDescription)
    {
        roleImage = image;
        title = roleTitle;
        description = roleDescription;
    }
}

public class RolesPageManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Button Settings")]
    [SerializeField] private List<Button> roleButtons = new List<Button>();
    [SerializeField] private Sprite normalButtonSprite;
    [SerializeField] private Sprite selectedButtonSprite;
    
    [Header("Role Data")]
    [SerializeField] private List<RoleItem> roleItems = new List<RoleItem>();
    
    private int currentSelectedIndex = -1;
    
    private void Start()
    {
        InitializeButtons();
        
        // Select the first item by default if available
        if (roleItems.Count > 0 && roleButtons.Count > 0)
        {
            SelectRole(0);
        }
    }
    
    private void InitializeButtons()
    {
        for (int i = 0; i < roleButtons.Count && i < roleItems.Count; i++)
        {
            int index = i; // Capture the index for the closure
            roleButtons[i].onClick.AddListener(() => SelectRole(index));
        }
    }
    
    public void SelectRole(int index)
    {
        if (index < 0 || index >= roleItems.Count || index >= roleButtons.Count)
        {
            Debug.LogWarning("Invalid role index: " + index);
            return;
        }
        Debug.Log("Role index: " + index);
        // Update the display elements
        RoleItem selectedRole = roleItems[index];
        displayImage.sprite = selectedRole.roleImage;
        titleText.text = selectedRole.title;
        descriptionText.text = selectedRole.description;
        
        // Update button visuals
        UpdateButtonVisuals(index);
        
        currentSelectedIndex = index;
    }
    
    private void UpdateButtonVisuals(int selectedIndex)
    {
        for (int i = 0; i < roleButtons.Count; i++)
        {
            Image buttonImage = roleButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = (i == selectedIndex) ? selectedButtonSprite : normalButtonSprite;
            }
        }
    }
    
    // Public method to add a new role item programmatically
    public void AddRoleItem(Sprite image, string title, string description)
    {
        RoleItem newRole = new RoleItem(image, title, description);
        roleItems.Add(newRole);
    }
    
    // Public method to get the currently selected role
    public RoleItem GetSelectedRole()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < roleItems.Count)
        {
            return roleItems[currentSelectedIndex];
        }
        return null;
    }
    
    // Public method to programmatically select a role by title
    public void SelectRoleByTitle(string title)
    {
        for (int i = 0; i < roleItems.Count; i++)
        {
            if (roleItems[i].title == title)
            {
                SelectRole(i);
                break;
            }
        }
    }
    
    private void OnValidate()
    {
        // Ensure we have matching numbers of buttons and role items in the inspector
        if (roleButtons.Count != roleItems.Count)
        {
            Debug.LogWarning("Number of role buttons and role items should match!");
        }
    }
}
