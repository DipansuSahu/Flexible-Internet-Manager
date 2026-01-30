/*
 * WebGLInternetCheck.jslib
 * JavaScript plugin for Unity WebGL to access browser's online/offline functionality
 * 
 * Installation:
 * Place this file in: Assets/Plugins/WebGL/WebGLInternetCheck.jslib
 */

mergeInto(LibraryManager.library, {
    
    /*
     * Check if browser is currently online
     * Uses navigator.onLine which is instant and native
     */
    IsOnline: function() {
        console.log('[WebGL] IsOnline called: ' + navigator.onLine);
        return navigator.onLine ? 1 : 0;
    },
    
    /*
     * Register callback for when browser goes online
     * Uses Unity's built-in SendMessage which works reliably
     */
    RegisterOnlineCallback: function(objectNamePtr, methodNamePtr) {
        var objectName = UTF8ToString(objectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        
        console.log('[WebGL] Registering online callback: ' + objectName + '.' + methodName);
        
        // Create event listener that uses SendMessage
        window.unityOnlineListener = function() {
            console.log('[WebGL] Browser online event detected');
            try {
                // SendMessage is Unity's built-in global function
                SendMessage(objectName, methodName);
            } catch (e) {
                console.error('[WebGL] Error sending online message:', e);
            }
        };
        
        window.addEventListener('online', window.unityOnlineListener);
    },
    
    /*
     * Register callback for when browser goes offline
     * Uses Unity's built-in SendMessage which works reliably
     */
    RegisterOfflineCallback: function(objectNamePtr, methodNamePtr) {
        var objectName = UTF8ToString(objectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        
        console.log('[WebGL] Registering offline callback: ' + objectName + '.' + methodName);
        
        // Create event listener that uses SendMessage
        window.unityOfflineListener = function() {
            console.log('[WebGL] Browser offline event detected');
            try {
                // SendMessage is Unity's built-in global function
                SendMessage(objectName, methodName);
            } catch (e) {
                console.error('[WebGL] Error sending offline message:', e);
            }
        };
        
        window.addEventListener('offline', window.unityOfflineListener);
    },
    
    /*
     * Unregister all callbacks
     * Call this when Unity object is destroyed
     */
    UnregisterCallbacks: function() {
        console.log('[WebGL] Unregistering callbacks');
        
        if (window.unityOnlineListener) {
            window.removeEventListener('online', window.unityOnlineListener);
            window.unityOnlineListener = null;
        }
        
        if (window.unityOfflineListener) {
            window.removeEventListener('offline', window.unityOfflineListener);
            window.unityOfflineListener = null;
        }
    }
});