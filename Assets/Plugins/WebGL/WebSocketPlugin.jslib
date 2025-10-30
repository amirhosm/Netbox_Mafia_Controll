var WebSocketPlugin = {
    $webSocketInstances: {},
    $webSocketNextId: 0,

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
                state: 0 // CONNECTING
            };
            
            socket.onopen = function() {
                console.log('[WebSocket] Connection opened: ' + url);
                webSocketInstances[id].state = 1; // OPEN
            };
            
            socket.onmessage = function(event) {
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
                webSocketInstances[id].state = 3; // CLOSED
            };
            
            socket.onclose = function(event) {
                console.log('[WebSocket] Closed. Code:', event.code, 'Reason:', event.reason);
                webSocketInstances[id].state = 3; // CLOSED
            };
            
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
            return;
        }
        
        var socket = webSocketInstances[socketId].socket;
        
        if (socket) {
            console.log('[WebSocket] Closing socket:', socketId);
            socket.close();
        }
        
        delete webSocketInstances[socketId];
    }
};

autoAddDeps(WebSocketPlugin, '$webSocketInstances');
autoAddDeps(WebSocketPlugin, '$webSocketNextId');
mergeInto(LibraryManager.library, WebSocketPlugin);