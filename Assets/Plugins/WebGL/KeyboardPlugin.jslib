var KeyboardPlugin = {
    $keyboardState: {
        inputElement: null,
        isOpen: false,
        unityInstance: null
    },

    OpenInputKeyboard: function(strPtr) {
        var initialValue = UTF8ToString(strPtr);
        console.log('[KeyboardPlugin] OpenInputKeyboard called with value:', initialValue);
        
        try {
            // Store Unity instance reference
            if (typeof unityInstance !== 'undefined') {
                keyboardState.unityInstance = unityInstance;
            } else if (typeof gameInstance !== 'undefined') {
                keyboardState.unityInstance = gameInstance;
            }

            // Create input element if it doesn't exist
            if (!keyboardState.inputElement) {
                console.log('[KeyboardPlugin] Creating input element...');
                
                keyboardState.inputElement = document.createElement('input');
                keyboardState.inputElement.type = 'text';
                keyboardState.inputElement.id = 'unity-webgl-keyboard-input';
                keyboardState.inputElement.setAttribute('autocomplete', 'off');
                keyboardState.inputElement.setAttribute('autocorrect', 'off');
                keyboardState.inputElement.setAttribute('autocapitalize', 'off');
                keyboardState.inputElement.setAttribute('spellcheck', 'false');
                
                // Style the input to be visible at bottom
                keyboardState.inputElement.style.position = 'fixed';
                keyboardState.inputElement.style.bottom = '0px';
                keyboardState.inputElement.style.left = '0px';
                keyboardState.inputElement.style.width = '100%';
                keyboardState.inputElement.style.height = '50px';
                keyboardState.inputElement.style.fontSize = '16px';
                keyboardState.inputElement.style.border = '2px solid #007bff';
                keyboardState.inputElement.style.padding = '10px';
                keyboardState.inputElement.style.zIndex = '9999';
                keyboardState.inputElement.style.boxSizing = 'border-box';
                keyboardState.inputElement.style.backgroundColor = '#ffffff';
                keyboardState.inputElement.style.color = '#000000';
                
                // Add event listener for input changes
                keyboardState.inputElement.addEventListener('input', function(e) {
                    var value = e.target.value || '';
                    console.log('[KeyboardPlugin] Input changed:', value);
                    
                    // Try multiple methods to send message to Unity
                    var messageSent = false;
                    
                    // Method 1: Modern Unity SendMessage
                    if (keyboardState.unityInstance && keyboardState.unityInstance.SendMessage) {
                        try {
                            keyboardState.unityInstance.SendMessage('_WebGLKeyboard', 'ReceiveInputChange', value);
                            messageSent = true;
                            console.log('[KeyboardPlugin] Message sent via unityInstance.SendMessage');
                        } catch (e) {
                            console.error('[KeyboardPlugin] Error with unityInstance.SendMessage:', e);
                        }
                    }
                    
                    // Method 2: Legacy SendMessage
                    if (!messageSent && typeof SendMessage !== 'undefined') {
                        try {
                            SendMessage('_WebGLKeyboard', 'ReceiveInputChange', value);
                            messageSent = true;
                            console.log('[KeyboardPlugin] Message sent via SendMessage');
                        } catch (e) {
                            console.error('[KeyboardPlugin] Error with SendMessage:', e);
                        }
                    }
                    
                    // Method 3: Try Module
                    if (!messageSent && typeof Module !== 'undefined' && Module.SendMessage) {
                        try {
                            Module.SendMessage('_WebGLKeyboard', 'ReceiveInputChange', value);
                            messageSent = true;
                            console.log('[KeyboardPlugin] Message sent via Module.SendMessage');
                        } catch (e) {
                            console.error('[KeyboardPlugin] Error with Module.SendMessage:', e);
                        }
                    }
                    
                    if (!messageSent) {
                        console.error('[KeyboardPlugin] Failed to send message to Unity - no SendMessage method available');
                        console.log('[KeyboardPlugin] Available globals:', Object.keys(window));
                    }
                });
                
                // Add blur event
                keyboardState.inputElement.addEventListener('blur', function() {
                    console.log('[KeyboardPlugin] Input blurred');
                });
                
                // Add to DOM
                document.body.appendChild(keyboardState.inputElement);
                console.log('[KeyboardPlugin] Input element created and added to DOM');
            } else {
                console.log('[KeyboardPlugin] Input element already exists, reusing');
                keyboardState.inputElement.style.display = 'block';
            }
            
            // Set value and focus
            keyboardState.inputElement.value = initialValue || '';
            keyboardState.inputElement.focus();
            keyboardState.isOpen = true;
            
            // Force keyboard on mobile
            if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
                console.log('[KeyboardPlugin] Mobile device detected, ensuring focus...');
                setTimeout(function() {
                    keyboardState.inputElement.focus();
                    keyboardState.inputElement.click();
                }, 100);
            }
            
            console.log('[KeyboardPlugin] Input focused, keyboard should be visible');
        } catch (e) {
            console.error('[KeyboardPlugin] Error in OpenInputKeyboard:', e);
            console.error('[KeyboardPlugin] Stack trace:', e.stack);
        }
    },

    CloseInputKeyboard: function() {
        console.log('[KeyboardPlugin] CloseInputKeyboard called');
        
        try {
            if (keyboardState.inputElement) {
                keyboardState.inputElement.blur();
                keyboardState.inputElement.style.display = 'none';
                keyboardState.isOpen = false;
                console.log('[KeyboardPlugin] Keyboard closed');
            } else {
                console.warn('[KeyboardPlugin] Cannot close - input element does not exist');
            }
        } catch (e) {
            console.error('[KeyboardPlugin] Error in CloseInputKeyboard:', e);
        }
    },

    FixInputOnBlur: function() {
        console.log('[KeyboardPlugin] FixInputOnBlur called');
    },

    FixInputUpdate: function() {
        console.log('[KeyboardPlugin] FixInputUpdate called');
    }
};

autoAddDeps(KeyboardPlugin, '$keyboardState');
mergeInto(LibraryManager.library, KeyboardPlugin);