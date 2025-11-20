var WebSocketPlugin = {
    $webSocketInstances: {},
    $webSocketNextId: 0,
    $pageVisibilityHandler: null,

    WebSocketConnect: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        var id = webSocketNextId++;
        
        console.log('[WebSocket] Connecting to: ' + url + ' with ID: ' + id);
        
        try {
            var socket = new WebSocket(url);
            socket.binaryType = 'arraybuffer';
            
            webSocketInstances[id] = {
                socket: socket,
                messages: [],
                state: 0, // CONNECTING
                url: url, // Store URL for reconnection
                lastActivity: Date.now()
            };
            
            socket.onopen = function() {
                console.log('[WebSocket] Connection opened: ' + url);
                // FIX: Check if instance still exists
                if (webSocketInstances[id]) {
                    webSocketInstances[id].state = 1; // OPEN
                    webSocketInstances[id].lastActivity = Date.now();
                }
            };
            
            socket.onmessage = function(event) {
                // FIX: Check if instance still exists
                if (!webSocketInstances[id]) {
                    console.warn('[WebSocket] Message received but instance ' + id + ' no longer exists');
                    return;
                }
                
                webSocketInstances[id].lastActivity = Date.now();
                
                console.log('[WebSocket] Message received, type:', typeof event.data);
                var data;
                
                if (event.data instanceof ArrayBuffer) {
                    data = new Uint8Array(event.data);
                } else if (typeof event.data === 'string') {
                    var encoder = new TextEncoder();
                    data = encoder.encode(event.data);
                } else {
                    console.error('[WebSocket] Unknown message type');
                    return;
                }
                
                webSocketInstances[id].messages.push(data);
                console.log('[WebSocket] Message queued, length:', data.length);
            };
            
            socket.onerror = function(error) {
                console.error('[WebSocket] Error:', error);
                // FIX: Check if instance still exists
                if (webSocketInstances[id]) {
                    webSocketInstances[id].state = 3; // CLOSED
                }
            };
            
            socket.onclose = function(event) {
                console.log('[WebSocket] Closed. Code:', event.code, 'Reason:', event.reason);
                // FIX: Check if instance still exists before setting state
                if (webSocketInstances[id]) {
                    webSocketInstances[id].state = 3; // CLOSED
                } else {
                    console.warn('[WebSocket] Close event for already deleted instance:', id);
                }
            };
            
            // CRITICAL: Add Page Visibility API handler (only once)
            if (!pageVisibilityHandler) {
                pageVisibilityHandler = function() {
                    if (document.hidden) {
                        console.log('[WebSocket] Page hidden - connections may suspend');
                    } else {
                        console.log('[WebSocket] Page visible - checking connections');
                        // Check all connections when page becomes visible
                        for (var socketId in webSocketInstances) {
                            var instance = webSocketInstances[socketId];
                            if (instance.socket && instance.socket.readyState !== 1) {
                                console.warn('[WebSocket] Connection ' + socketId + ' not open after resume, state:', instance.socket.readyState);
                            }
                        }
                    }
                };
                
                document.addEventListener('visibilitychange', pageVisibilityHandler);
                console.log('[WebSocket] Visibility change handler registered');
                
                // Also handle pagehide/pageshow for iOS Safari
                window.addEventListener('pagehide', function() {
                    console.log('[WebSocket] Page hiding');
                });
                
                window.addEventListener('pageshow', function(e) {
                    console.log('[WebSocket] Page showing, persisted:', e.persisted);
                });
            }
            
            return id;
        } catch (e) {
            console.error('[WebSocket] Creation failed:', e);
            return -1;
        }
    },

    WebSocketGetState: function(socketId) {
        if (!webSocketInstances[socketId]) {
            console.warn('[WebSocket] GetState: Invalid socket ID:', socketId);
            return 3; // CLOSED
        }
        return webSocketInstances[socketId].state;
    },

    WebSocketSend: function(socketId, bufferPtr, length) {
        if (!webSocketInstances[socketId]) {
            console.error('[WebSocket] Send: Invalid socket ID:', socketId);
            return;
        }
        
        var socket = webSocketInstances[socketId].socket;
        
        if (socket && socket.readyState === 1) { // WebSocket.OPEN
            try {
                var buffer = new Uint8Array(Module.HEAPU8.buffer, bufferPtr, length);
                var arrayCopy = new Uint8Array(buffer); // Create a copy
                socket.send(arrayCopy);
                console.log('[WebSocket] Sent', length, 'bytes');
                
                // Update last activity
                if (webSocketInstances[socketId]) {
                    webSocketInstances[socketId].lastActivity = Date.now();
                }
            } catch (e) {
                console.error('[WebSocket] Send error:', e);
            }
        } else {
            console.warn('[WebSocket] Cannot send, socket not ready. State:', socket ? socket.readyState : 'null');
        }
    },

    WebSocketSendText: function(socketId, messagePtr) {
        if (!webSocketInstances[socketId]) {
            console.error('[WebSocket] SendText: Invalid socket ID:', socketId);
            return;
        }
        
        var socket = webSocketInstances[socketId].socket;
        var message = UTF8ToString(messagePtr);
        
        if (socket && socket.readyState === 1) {
            socket.send(message);
            console.log('[WebSocket] Sent text:', message.substring(0, 50));
            
            // Update last activity
            if (webSocketInstances[socketId]) {
                webSocketInstances[socketId].lastActivity = Date.now();
            }
        }
    },

    WebSocketReceive: function(socketId, bufferPtr, bufferSize) {
        if (!webSocketInstances[socketId]) {
            return 0;
        }
        
        var messages = webSocketInstances[socketId].messages;
        
        if (messages.length === 0) {
            return 0;
        }
        
        var message = messages.shift();
        var length = Math.min(message.length, bufferSize);
        
        Module.HEAPU8.set(message.subarray(0, length), bufferPtr);
        console.log('[WebSocket] Received', length, 'bytes from queue');
        
        return length;
    },

    WebSocketClose: function(socketId) {
        if (!webSocketInstances[socketId]) {
            console.warn('[WebSocket] Close: Instance ' + socketId + ' already deleted');
            return;
        }
        
        var instance = webSocketInstances[socketId];
        var socket = instance.socket;
        
        if (socket) {
            console.log('[WebSocket] Closing socket:', socketId);
            
            // CRITICAL FIX: Remove event handlers to prevent callbacks after deletion
            socket.onopen = null;
            socket.onmessage = null;
            socket.onerror = null;
            socket.onclose = null;
            
            // Close the socket
            if (socket.readyState === 0 || socket.readyState === 1) { // CONNECTING or OPEN
                try {
                    socket.close();
                } catch (e) {
                    console.warn('[WebSocket] Error closing socket:', e);
                }
            }
        }
        
        // Delete the instance
        delete webSocketInstances[socketId];
        console.log('[WebSocket] Instance ' + socketId + ' deleted');
    }
};

autoAddDeps(WebSocketPlugin, '$webSocketInstances');
autoAddDeps(WebSocketPlugin, '$webSocketNextId');
autoAddDeps(WebSocketPlugin, '$pageVisibilityHandler');
mergeInto(LibraryManager.library, WebSocketPlugin);