mergeInto(LibraryManager.library, {
    OpenFilePicker: function(gameObjectNamePtr, callbackMethodPtr) {
        // Convert pointers to strings
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        
        console.log("[FileUploader] Opening file picker for:", gameObjectName, "callback:", callbackMethod);
        
        // Create file input element
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.style.display = 'none';
        document.body.appendChild(input);
        
        input.onchange = function(event) {
            console.log("[FileUploader] File selected");
            var file = event.target.files[0];
            
            if (file) {
                console.log("[FileUploader] File details:", file.name, file.size, "bytes");
                
                var reader = new FileReader();
                
                reader.onload = function(e) {
                    try {
                        console.log("[FileUploader] File loaded, size:", e.target.result.byteLength);
                        
                        // Get the array buffer
                        var arrayBuffer = e.target.result;
                        var bytes = new Uint8Array(arrayBuffer);
                        
                        console.log("[FileUploader] Bytes array length:", bytes.length);
                        
                        // Allocate memory in Unity's heap
                        var buffer = _malloc(bytes.length);
                        
                        // Copy bytes to Unity's heap
                        HEAPU8.set(bytes, buffer);
                        
                        // Create the data string: "pointer,length"
                        var dataString = buffer + ',' + bytes.length;
                        
                        console.log("[FileUploader] Sending to Unity:", dataString);
                        
                        // Send message to Unity
                        SendMessage(gameObjectName, callbackMethod, dataString);
                        
                        console.log("[FileUploader] ✓ Message sent to Unity");
                        
                        // Free the allocated memory after a short delay
                        setTimeout(function() {
                            _free(buffer);
                            console.log("[FileUploader] Memory freed");
                        }, 1000);
                        
                    } catch (error) {
                        console.error("[FileUploader] Error processing file:", error);
                        SendMessage(gameObjectName, callbackMethod, "0,0");
                    }
                };
                
                reader.onerror = function(error) {
                    console.error("[FileUploader] FileReader error:", error);
                    SendMessage(gameObjectName, callbackMethod, "0,0");
                };
                
                // Read file as ArrayBuffer
                reader.readAsArrayBuffer(file);
            } else {
                console.log("[FileUploader] No file selected");
            }
            
            // Remove the input element
            document.body.removeChild(input);
        };
        
        input.oncancel = function() {
            console.log("[FileUploader] File selection cancelled");
            document.body.removeChild(input);
        };
        
        // Trigger the file picker
        input.click();
    }
});