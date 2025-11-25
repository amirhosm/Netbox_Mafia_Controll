using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarPanel : MonoBehaviour
{
    [SerializeField] private Sprite Selected, Deselected;
    [SerializeField] private List<Image> avatarBackground;

    private int currentlySelectedIndex = -1;

    private void Start()
    {
        // Initialize all buttons to deselected state
        for (int i = 0; i < avatarBackground.Count; i++)
        {
            int index = i; // Capture index for closure
            avatarBackground[i].sprite = Deselected;
        }
    }

    /// <summary>
    /// Called when any avatar button is clicked
    /// </summary>
    /// <param name="index">Index of the clicked button</param>
    public void OnAvatarClicked(int index)
    {
        DeselectAll();

        // Deselect all buttons
        for (int i = 0; i < avatarBackground.Count; i++)
        {
            if ((i==index))
            {
                avatarBackground[i].sprite = Selected;
            }            
        }
        currentlySelectedIndex = index;
    }       

    /// <summary>
    /// Reset all buttons to deselected state
    /// </summary>
    public void DeselectAll()
    {
        for (int i = 0; i < avatarBackground.Count; i++)
        {
            avatarBackground[i].sprite = Deselected;
        }
        currentlySelectedIndex = -1;
    }

    private void OnDisable()
    {
        DeselectAll();
    }
}