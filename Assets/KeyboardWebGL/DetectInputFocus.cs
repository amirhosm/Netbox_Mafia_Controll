using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Scripting;

namespace WebGLKeyboard
{
    /// <summary>
    /// Trigger the focus event in input fields
    /// </summary>
    [Preserve]
    public class DetectInputFocus : MonoBehaviour, IPointerClickHandler, IDeselectHandler
    {
        private KeyboardController controller = null;
        private TMP_InputField tmproInput;

        [Preserve]
        public void Initialize(KeyboardController _controller)
        {
            if (_controller == null)
            {
                Debug.LogError("[DetectInputFocus] Initialize called with null controller!");
                return;
            }

            controller = _controller;
            tmproInput = gameObject.GetComponent<TMP_InputField>();
            
            Debug.Log($"[DetectInputFocus] Initialized on {gameObject.name} - TMP: {tmproInput != null}, Controller: {controller != null}");
            
            if (tmproInput == null)
            {
                Debug.LogError($"[DetectInputFocus] No TMP_InputField found on {gameObject.name}!");
            }
        }
        
        /// <summary>
        /// Calls the controller passing the selected input field to enable the keyboard
        /// </summary>
        /// <param name="_data"></param>
        [Preserve]
        public void OnPointerClick(PointerEventData _data)
        {
            Debug.Log($"[DetectInputFocus] OnPointerClick on {gameObject.name}");
            
            if (controller == null)
            {
                Debug.LogError("[DetectInputFocus] Controller is null! Trying to find it...");
                
                // Try to find the controller if it's null
                GameObject controllerObj = GameObject.Find("_WebGLKeyboard");
                if (controllerObj != null)
                {
                    controller = controllerObj.GetComponent<KeyboardController>();
                    Debug.Log($"[DetectInputFocus] Found controller: {controller != null}");
                }
                
                if (controller == null)
                {
                    Debug.LogError("[DetectInputFocus] Still cannot find controller!");
                    return;
                }
            }
            
            if (tmproInput != null)
            {
                Debug.Log($"[DetectInputFocus] Calling FocusInput for {tmproInput.gameObject.name}");
                controller.FocusInput(tmproInput);
            }
            else
            {
                Debug.LogError($"[DetectInputFocus] TMP_InputField is null on {gameObject.name}!");
            }
        }
        
        /// <summary>
        /// Clears the input action if the player deselected the field ingame
        /// </summary>
        /// <param name="data"></param>
        [Preserve]
        public void OnDeselect(BaseEventData data)
        {
            Debug.Log($"[DetectInputFocus] OnDeselect on {gameObject.name}");
            
            if (controller != null)
            {
                controller.ForceClose();
            }
            else
            {
                Debug.LogWarning("[DetectInputFocus] Cannot close keyboard - controller is null");
            }
        }
    }
}