using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BtnCTR : MonoBehaviour
{
    public string str;
    public TMP_InputField ipInput;
    public bool isDeleteButton = false;
    
    public void onClick()
    {
        if (isDeleteButton)
        {
            // Delete last character
            DeleteLastCharacter();
        }
        else
        {
            // Add character
            ipInput.text += str;
        }
    }

    private void DeleteLastCharacter()
    {
        if (!string.IsNullOrEmpty(ipInput.text))
        {
            ipInput.text = ipInput.text.Substring(0, ipInput.text.Length - 1);
            Debug.Log($"[BtnCTR] Deleted last character. Current text: '{ipInput.text}'");
        }
        else
        {
            Debug.Log("[BtnCTR] Input field is already empty, nothing to delete");
        }
    }
}
