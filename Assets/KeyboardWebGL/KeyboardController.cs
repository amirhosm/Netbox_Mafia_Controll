using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine.Scripting;

namespace WebGLKeyboard
{
    /// <summary>
    /// Controls the flow of opening the keyboard and adding the necessary components to input fields as scenes load
    /// </summary>
    [Preserve]
    public class KeyboardController : MonoBehaviour
    {
        [Preserve]
        public bool isKeyboardOpen = false;
        
        [Preserve]
        public TMP_InputField currentTmproInput;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OpenInputKeyboard(string str);
        [DllImport("__Internal")]
        private static extern void CloseInputKeyboard();
        
        //Just adds these functions references to avoid stripping
        [DllImport("__Internal")]
        private static extern void FixInputOnBlur();
        [DllImport("__Internal")]
        private static extern void FixInputUpdate();
#endif

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Start()
        {
            PadronizeObjectName();
            Debug.Log("[KeyboardController] Start called, initializing...");
            
            //Calls the scene loaded in the first scene manually because this component will initialize after the scene load
            StartCoroutine(InitializeAfterFrame());

            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator InitializeAfterFrame()
        {
            // Wait one frame to ensure all objects are loaded
            yield return null;
            
            Debug.Log("[KeyboardController] Delayed initialization starting...");
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        
        /// <summary>
        /// Changes this object name and parent to guarantee that it will be accessible from the outside javascript functions
        /// </summary>
        private void PadronizeObjectName()
        {
            gameObject.name = "_WebGLKeyboard";
            gameObject.transform.SetParent(null);
            Debug.Log("[KeyboardController] Object name set to _WebGLKeyboard");
        }
        
        /// <summary>
        /// Callback when scene loads to add the DetectFocus component to every input field
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        [Preserve]
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[KeyboardController] Scene loaded: {scene.name}, searching for TMP_InputFields...");

            List<TMP_InputField> tmProInputs = FindObjectsOfTypeInScene<TMP_InputField>(scene);
            Debug.Log($"[KeyboardController] Found {tmProInputs.Count} TMP_InputFields");
            
            for (int x = 0; x < tmProInputs.Count; x++)
            {
                if (tmProInputs[x] == null)
                {
                    Debug.LogWarning($"[KeyboardController] TMP_InputField at index {x} is null, skipping");
                    continue;
                }

                // Check if it already has DetectInputFocus to avoid duplicates
                DetectInputFocus existing = tmProInputs[x].gameObject.GetComponent<DetectInputFocus>();
                if (existing == null)
                {
                    Debug.Log($"[KeyboardController] Adding DetectInputFocus to: {tmProInputs[x].gameObject.name}");
                    DetectInputFocus detect = tmProInputs[x].gameObject.AddComponent<DetectInputFocus>();
                    detect.Initialize(this);
                    Debug.Log($"[KeyboardController] Successfully added and initialized DetectInputFocus on: {tmProInputs[x].gameObject.name}");
                }
                else
                {
                    Debug.Log($"[KeyboardController] DetectInputFocus already exists on: {tmProInputs[x].gameObject.name}, re-initializing");
                    existing.Initialize(this);
                }
            }

            if (tmProInputs.Count == 0)
            {
                Debug.LogWarning("[KeyboardController] No TMP_InputFields found in scene! Make sure your input fields are active in the hierarchy.");
            }
        }
        
        /// <summary>
        /// Call the external javascript function to trigger the keyboard and link to the input field
        /// </summary>
        /// <param name="input"></param>
        [Preserve]
        public void FocusInput(TMP_InputField input)
        {
            if (input == null)
            {
                Debug.LogError("[KeyboardController] FocusInput called with null TMP_InputField!");
                return;
            }

            Debug.Log($"[KeyboardController] FocusInput called for TMP_InputField: {input.gameObject.name}");
            isKeyboardOpen = true;
            currentTmproInput = input;
            
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string currentText = input.text ?? "";
                Debug.Log($"[KeyboardController] Opening keyboard with text: '{currentText}'");
                OpenInputKeyboard(currentText);
                UnityEngine.WebGLInput.captureAllKeyboardInput = false;
                Debug.Log("[KeyboardController] Keyboard opened successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[KeyboardController] Error opening keyboard: {e.Message}\n{e.StackTrace}");
            }
#else
            Debug.LogWarning("[KeyboardController] Not in WebGL build, keyboard won't open (running in Editor)");
#endif
        }
        
        /// <summary>
        /// Forces the keyboard to close and unfocus
        /// </summary>
        [Preserve]
        public void ForceClose()
        {
            Debug.Log("[KeyboardController] ForceClose called");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                CloseInputKeyboard();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[KeyboardController] Error closing keyboard: {e.Message}");
            }
#endif
        }
        
        /// <summary>
        /// Clear the link to the open keyboard
        /// </summary>
        [Preserve]
        public void LoseFocus()
        {
            if (!isKeyboardOpen)
                return;

            Debug.Log("[KeyboardController] LoseFocus called");
            isKeyboardOpen = false;
            
            if (currentTmproInput != null)
            {
                currentTmproInput.DeactivateInputField();
                currentTmproInput = null;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            UnityEngine.WebGLInput.captureAllKeyboardInput = true;
#endif
        }
        
        /// <summary>
        /// Receives the string inputed in the keyboard
        /// </summary>
        /// <param name="value"></param>
        [Preserve]
        public void ReceiveInputChange(string value)
        {
            Debug.Log($"[KeyboardController] ReceiveInputChange: '{value}'");

            if (!isKeyboardOpen)
            {
                Debug.LogWarning("[KeyboardController] Keyboard not open, ignoring input");
                return;
            }

            if (currentTmproInput != null)
            {
                currentTmproInput.text = value;
                Debug.Log($"[KeyboardController] Updated input field '{currentTmproInput.gameObject.name}' with text: '{value}'");
            }
            else
            {
                Debug.LogWarning("[KeyboardController] currentTmproInput is null, cannot update text");
            }
        }
        
        /// <summary>
        /// Returns all objects of a type in a loaded scene
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Preserve]
        public List<T> FindObjectsOfTypeInScene<T>(Scene scene)
        {
            List<T> results = new List<T>();
            if (scene.isLoaded)
            {
                var allGameObjects = scene.GetRootGameObjects();
                for (int x = 0; x < allGameObjects.Length; x++)
                {
                    var go = allGameObjects[x];
                    results.AddRange(go.GetComponentsInChildren<T>(true));
                }
            }
            return results;
        }
    }
}