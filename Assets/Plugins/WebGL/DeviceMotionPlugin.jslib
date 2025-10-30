var DeviceMotionPlugin = {
    $deviceMotionState: {
        isListening: false,
        alpha: 0,
        beta: 0,
        gamma: 0,
        hasPermission: false,
        lastBeta: 0,
        lastGamma: 0,
        eventCount: 0,
        initPromise: null
    },

    InitDeviceMotion: function() {
        console.log('[DeviceMotion] ===== InitDeviceMotion called =====');
        console.log('[DeviceMotion] User agent:', navigator.userAgent);
        
        // Check if DeviceOrientationEvent exists
        if (typeof DeviceOrientationEvent === 'undefined') {
            console.error('[DeviceMotion] DeviceOrientation API not supported in this browser');
            return -1;
        }

        console.log('[DeviceMotion] DeviceOrientation API available');
        console.log('[DeviceMotion] Checking for requestPermission function...');

        // iOS 13+ requires permission
        if (typeof DeviceOrientationEvent.requestPermission === 'function') {
            console.log('[DeviceMotion] iOS 13+ detected - requesting permission NOW...');
            
            // Call permission request SYNCHRONOUSLY from user interaction
            DeviceOrientationEvent.requestPermission()
                .then(function(response) {
                    console.log('[DeviceMotion] Permission response:', response);
                    if (response === 'granted') {
                        console.log('[DeviceMotion] ✓✓✓ Permission GRANTED ✓✓✓');
                        deviceMotionState.hasPermission = true;
                        startListening();
                        
                        // Try to notify Unity that permission was granted
                        console.log('[DeviceMotion] Permission granted asynchronously');
                    } else {
                        console.error('[DeviceMotion] ✗✗✗ Permission DENIED ✗✗✗');
                        deviceMotionState.hasPermission = false;
                    }
                })
                .catch(function(error) {
                    console.error('[DeviceMotion] Permission request ERROR:', error);
                    deviceMotionState.hasPermission = false;
                });
                
            // Return 1 immediately - permission will be granted asynchronously
            console.log('[DeviceMotion] Returning 1 (permission request initiated)');
            return 1;
        } else {
            // Non-iOS or older iOS - no permission needed
            console.log('[DeviceMotion] No permission required - starting listener immediately');
            deviceMotionState.hasPermission = true;
            startListening();
            return 1;
        }

        function startListening() {
            if (!deviceMotionState.isListening) {
                console.log('[DeviceMotion] Starting event listener...');
                window.addEventListener('deviceorientation', handleOrientation, true);
                deviceMotionState.isListening = true;
                console.log('[DeviceMotion] ✓ Event listener ACTIVE');
                
                // Test after 2 seconds
                setTimeout(function() {
                    console.log('[DeviceMotion] ===== STATUS CHECK =====');
                    console.log('[DeviceMotion] Events received:', deviceMotionState.eventCount);
                    console.log('[DeviceMotion] Is listening:', deviceMotionState.isListening);
                    console.log('[DeviceMotion] Has permission:', deviceMotionState.hasPermission);
                    console.log('[DeviceMotion] Current values:');
                    console.log('  Alpha:', deviceMotionState.alpha);
                    console.log('  Beta:', deviceMotionState.beta);
                    console.log('  Gamma:', deviceMotionState.gamma);
                    
                    if (deviceMotionState.eventCount === 0) {
                        console.error('[DeviceMotion] ✗ NO EVENTS RECEIVED - Gyro may not be working!');
                        console.log('[DeviceMotion] Trying to request permission again...');
                        
                        // Try to add listener again
                        window.removeEventListener('deviceorientation', handleOrientation, true);
                        window.addEventListener('deviceorientation', handleOrientation, true);
                    }
                }, 2000);
            } else {
                console.log('[DeviceMotion] Already listening');
            }
        }

        function handleOrientation(event) {
            deviceMotionState.eventCount++;
            
            // Log first 10 events
            if (deviceMotionState.eventCount <= 10) {
                console.log('[DeviceMotion] Event #' + deviceMotionState.eventCount + ':', 
                    'alpha=' + (event.alpha ? event.alpha.toFixed(2) : 'null'), 
                    'beta=' + (event.beta ? event.beta.toFixed(2) : 'null'), 
                    'gamma=' + (event.gamma ? event.gamma.toFixed(2) : 'null'));
            }
            
            deviceMotionState.alpha = event.alpha || 0;
            deviceMotionState.beta = event.beta || 0;
            deviceMotionState.gamma = event.gamma || 0;
        }
    },

    GetDeviceTilt: function(outX, outY) {
        if (!deviceMotionState.isListening) {
            Module.HEAPF32[outX >> 2] = 0;
            Module.HEAPF32[outY >> 2] = 0;
            return 0;
        }

        // Calculate delta (change) from last frame
        var deltaBeta = deviceMotionState.beta - deviceMotionState.lastBeta;
        var deltaGamma = deviceMotionState.gamma - deviceMotionState.lastGamma;

        // Update last values
        deviceMotionState.lastBeta = deviceMotionState.beta;
        deviceMotionState.lastGamma = deviceMotionState.gamma;

        // Clamp deltas to avoid jumps
        deltaBeta = Math.max(-10, Math.min(10, deltaBeta));
        deltaGamma = Math.max(-10, Math.min(10, deltaGamma));

        // Write to Unity
        Module.HEAPF32[outX >> 2] = deltaGamma;  // Left/right tilt
        Module.HEAPF32[outY >> 2] = deltaBeta;   // Front/back tilt

        // Log occasionally
        if (deviceMotionState.eventCount % 120 === 0 && deviceMotionState.eventCount > 0) {
            console.log('[DeviceMotion] GetDeviceTilt called:', 
                'deltaGamma=' + deltaGamma.toFixed(2), 
                'deltaBeta=' + deltaBeta.toFixed(2),
                'totalEvents=' + deviceMotionState.eventCount);
        }

        return 1;
    },

    HasDeviceMotionPermission: function() {
        var hasPerm = (deviceMotionState.hasPermission && deviceMotionState.isListening) ? 1 : 0;
        
        // Only log occasionally to avoid spam
        if (deviceMotionState.eventCount % 300 === 0) {
            console.log('[DeviceMotion] HasDeviceMotionPermission check:', 
                'hasPermission=' + deviceMotionState.hasPermission,
                'isListening=' + deviceMotionState.isListening,
                'returning=' + hasPerm);
        }
        
        return hasPerm;
    },

    StopDeviceMotion: function() {
        console.log('[DeviceMotion] StopDeviceMotion called');
        if (deviceMotionState.isListening) {
            window.removeEventListener('deviceorientation', handleOrientation, true);
            deviceMotionState.isListening = false;
            console.log('[DeviceMotion] ✓ Stopped listening');
        }
    }
};

autoAddDeps(DeviceMotionPlugin, '$deviceMotionState');
mergeInto(LibraryManager.library, DeviceMotionPlugin);