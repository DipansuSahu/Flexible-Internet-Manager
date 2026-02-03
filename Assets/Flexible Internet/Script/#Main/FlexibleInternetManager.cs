using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Flexible Internet Check Manager with Multiple Strategies
/// Choose your preferred checking method:
/// - BrowserAPI: WebGL navigator.onLine (instant, WebGL only)
/// - UnityAPI: Application.internetReachability (instant, less reliable)
/// - HTTP: Web requests to multiple URLs (slow but reliable)
/// - UnityAndHTTP: Quick Unity check + HTTP verification (two-stage)
/// - BrowserAndHTTP: Browser check + HTTP verification (WebGL two-stage)
/// </summary>
public class FlexibleInternetManager : MonoBehaviour
{
    #region Singleton

    public static FlexibleInternetManager Instance;

    #endregion Singleton

    #region Checking Strategy Enum

    public enum CheckingStrategy
    {
        BrowserAPI,      // WebGL only: navigator.onLine + events (instant)
        UnityAPI,        // All platforms: Application.internetReachability (instant but less reliable)
        HTTP,            // All platforms: HTTP requests to URLs (slow but reliable)
        UnityAndHTTP,    // All platforms: Unity check first, then HTTP if network exists (two-stage)
        BrowserAndHTTP   // WebGL only: Browser check first, then HTTP verify (WebGL two-stage)
    }

    #endregion Checking Strategy Enum

    #region WebGL JavaScript Interface

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool IsOnline();

    [DllImport("__Internal")]
    private static extern void RegisterOnlineCallback(string objectName, string methodName);

    [DllImport("__Internal")]
    private static extern void RegisterOfflineCallback(string objectName, string methodName);

    [DllImport("__Internal")]
    private static extern void UnregisterCallbacks();
#else

    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;

    private static void RegisterOnlineCallback(string objectName, string methodName)
    { }

    private static void RegisterOfflineCallback(string objectName, string methodName)
    { }

    private static void UnregisterCallbacks()
    { }

#endif

    #endregion WebGL JavaScript Interface

    #region Events

    public event Action OnInternetConnected;

    public event Action OnInternetDisconnected;

    public event Action<bool> OnConnectionStatusChanged;

    #endregion Events

    #region Configuration

    [Header("Strategy Selection")]
    [Tooltip("Choose your preferred checking method")]
    [SerializeField] private CheckingStrategy strategy = CheckingStrategy.UnityAndHTTP;

    [Header("Auto Platform Optimization")]
    [Tooltip("Automatically choose best strategy for current platform")]
    [SerializeField] private bool autoOptimizeForPlatform = false;

    [Header("General Settings")]
    [SerializeField] private bool autoCheckOnStart = true;

    [SerializeField] private bool continuousMonitoring = true;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private float timeout = 10f;

    [Header("HTTP Settings")]
    [SerializeField]
    private string[] checkUrls = new string[]
    {
        "https://www.google.com",
        "https://www.cloudflare.com",
        "https://www.microsoft.com"
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [SerializeField] private bool showDetailedLogs = false;

    #endregion Configuration

    #region Private Variables

    private bool _hasInitialized = false;
    private bool _isConnected = false;
    private bool _isChecking = false;
    private Coroutine _monitoringCoroutine;
    private bool _isWebGL = false;
    private CheckingStrategy _activeStrategy;

    #endregion Private Variables

    #region Properties

    public bool IsConnected => _isConnected;
    public bool IsChecking => _isChecking;
    public CheckingStrategy CurrentStrategy => _activeStrategy;
    public bool IsWebGLBuild => _isWebGL;

    public float CheckInterval
    {
        get => checkInterval;
        set => checkInterval = Mathf.Max(0.5f, value);
    }

    #endregion Properties

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // Detect platform
#if UNITY_WEBGL && !UNITY_EDITOR
        _isWebGL = true;
#else
        _isWebGL = false;
#endif

        // Determine active strategy
        DetermineStrategy();
    }

    private void Start()
    {
        InitializeStrategy();
    }

    private void OnDestroy()
    {
        CleanupStrategy();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion Unity Lifecycle

    #region Strategy Determination

    private void DetermineStrategy()
    {
        if (autoOptimizeForPlatform)
        {
            _activeStrategy = GetOptimalStrategyForPlatform();
            Log($"Auto-optimized strategy: {_activeStrategy}");
        }
        else
        {
            _activeStrategy = strategy;

            // Validate strategy for platform
            if (!IsStrategyValidForPlatform(_activeStrategy))
            {
                Log($"Warning: Strategy '{_activeStrategy}' not optimal for current platform. Consider using auto-optimization.");
            }
        }

        LogStrategyInfo();
    }

    private CheckingStrategy GetOptimalStrategyForPlatform()
    {
        if (_isWebGL)
        {
            // WebGL: Browser API with optional HTTP verification
            return CheckingStrategy.BrowserAndHTTP;
        }
        else
        {
#if UNITY_ANDROID || UNITY_IOS
            // Mobile: Two-stage for battery efficiency
            return CheckingStrategy.UnityAndHTTP;
#else
            // Standalone/Editor: Pure HTTP for reliability
            return CheckingStrategy.HTTP;
#endif
        }
    }

    private bool IsStrategyValidForPlatform(CheckingStrategy strat)
    {
        switch (strat)
        {
            case CheckingStrategy.BrowserAPI:
            case CheckingStrategy.BrowserAndHTTP:
                return _isWebGL; // Only works in WebGL

            case CheckingStrategy.UnityAPI:
            case CheckingStrategy.HTTP:
            case CheckingStrategy.UnityAndHTTP:
                return true; // Works everywhere

            default:
                return true;
        }
    }

    private void LogStrategyInfo()
    {
        Log("=== Flexible Internet Manager ===");
        Log($"Platform: {Application.platform}");
        Log($"Active Strategy: {_activeStrategy}");
        Log($"Auto-Optimize: {autoOptimizeForPlatform}");

        switch (_activeStrategy)
        {
            case CheckingStrategy.BrowserAPI:
                Log("Method: Browser API Only");
                Log("  ⚡ Speed: < 1ms (instant)");
                Log("  🎯 Accuracy: Good");
                Log("  📡 Bandwidth: Zero");
                Log("  🔋 Battery: Minimal");
                Log("  🌐 Platform: WebGL only");
                break;

            case CheckingStrategy.UnityAPI:
                Log("Method: Unity API Only");
                Log("  ⚡ Speed: < 1ms (instant)");
                Log("  🎯 Accuracy: Fair (can give false positives)");
                Log("  📡 Bandwidth: Zero");
                Log("  🔋 Battery: Minimal");
                Log("  🌐 Platform: All");
                break;

            case CheckingStrategy.HTTP:
                Log("Method: HTTP Only");
                Log("  ⚡ Speed: 100ms-10s");
                Log("  🎯 Accuracy: Excellent");
                Log("  📡 Bandwidth: ~1-3KB per check");
                Log("  🔋 Battery: Higher");
                Log("  🌐 Platform: All");
                break;

            case CheckingStrategy.UnityAndHTTP:
                Log("Method: Unity API + HTTP Verification");
                Log("  ⚡ Speed: Instant (no network) / 100ms-10s (has network)");
                Log("  🎯 Accuracy: Excellent");
                Log("  📡 Bandwidth: ~1KB only when network exists");
                Log("  🔋 Battery: Low (skips HTTP when no network)");
                Log("  🌐 Platform: All");
                break;

            case CheckingStrategy.BrowserAndHTTP:
                Log("Method: Browser API + HTTP Verification");
                Log("  ⚡ Speed: < 1ms (browser) + 100ms-10s (verification)");
                Log("  🎯 Accuracy: Excellent");
                Log("  📡 Bandwidth: ~1KB for verification");
                Log("  🔋 Battery: Low");
                Log("  🌐 Platform: WebGL only");
                break;
        }
        Log("===============================");
    }

    #endregion Strategy Determination

    #region Strategy Initialization

    private void InitializeStrategy()
    {
        switch (_activeStrategy)
        {
            case CheckingStrategy.BrowserAPI:
                if (_isWebGL)
                {
                    InitializeBrowserAPI();
                }
                else
                {
                    Log("Error: BrowserAPI strategy requires WebGL platform. Falling back to HTTP.");
                    _activeStrategy = CheckingStrategy.HTTP;
                    InitializeHTTP();
                }
                break;

            case CheckingStrategy.UnityAPI:
                InitializeUnityAPI();
                break;

            case CheckingStrategy.HTTP:
                InitializeHTTP();
                break;

            case CheckingStrategy.UnityAndHTTP:
                InitializeUnityAndHTTP();
                break;

            case CheckingStrategy.BrowserAndHTTP:
                if (_isWebGL)
                {
                    InitializeBrowserAndHTTP();
                }
                else
                {
                    Log("Error: BrowserAndHTTP strategy requires WebGL platform. Falling back to UnityAndHTTP.");
                    _activeStrategy = CheckingStrategy.UnityAndHTTP;
                    InitializeUnityAndHTTP();
                }
                break;
        }
    }

    private void InitializeBrowserAPI()
    {
        Log("Initializing Browser API strategy...");
        RegisterOnlineCallback(gameObject.name, "OnBrowserOnline");
        RegisterOfflineCallback(gameObject.name, "OnBrowserOffline");

        // Initial check
        bool browserOnline = IsOnline();
        UpdateConnectionStatus(browserOnline);
    }

    private void InitializeUnityAPI()
    {
        Log("Initializing Unity API strategy...");

        if (autoCheckOnStart)
        {
            CheckWithUnityAPI();
        }

        if (continuousMonitoring)
        {
            StartMonitoring();
        }
    }

    private void InitializeHTTP()
    {
        Log("Initializing HTTP strategy...");

        if (autoCheckOnStart)
        {
            CheckWithHTTP();
        }

        if (continuousMonitoring)
        {
            StartMonitoring();
        }
    }

    private void InitializeUnityAndHTTP()
    {
        Log("Initializing Unity + HTTP strategy...");

        if (autoCheckOnStart)
        {
            CheckWithUnityAndHTTP();
        }

        if (continuousMonitoring)
        {
            StartMonitoring();
        }
    }

    private void InitializeBrowserAndHTTP()
    {
        Log("Initializing Browser + HTTP strategy...");
        RegisterOnlineCallback(gameObject.name, "OnBrowserOnlineWithVerification");
        RegisterOfflineCallback(gameObject.name, "OnBrowserOffline");

        // Initial check with verification
        CheckWithBrowserAndHTTP();
    }

    private void CleanupStrategy()
    {
        if (_activeStrategy == CheckingStrategy.BrowserAPI ||
            _activeStrategy == CheckingStrategy.BrowserAndHTTP)
        {
            UnregisterCallbacks();
        }

        if (_monitoringCoroutine != null)
        {
            StopCoroutine(_monitoringCoroutine);
        }
    }

    #endregion Strategy Initialization

    #region Public Methods

    /// <summary>
    /// Check internet connection using current strategy
    /// </summary>
    public void CheckInternetConnection()
    {
        switch (_activeStrategy)
        {
            case CheckingStrategy.BrowserAPI:
                CheckWithBrowserAPI();
                break;

            case CheckingStrategy.UnityAPI:
                CheckWithUnityAPI();
                break;

            case CheckingStrategy.HTTP:
                CheckWithHTTP();
                break;

            case CheckingStrategy.UnityAndHTTP:
                CheckWithUnityAndHTTP();
                break;

            case CheckingStrategy.BrowserAndHTTP:
                CheckWithBrowserAndHTTP();
                break;
        }
    }

    /// <summary>
    /// Change strategy at runtime
    /// </summary>
    public void SetStrategy(CheckingStrategy newStrategy)
    {
        if (newStrategy == _activeStrategy) return;

        Log($"Changing strategy from {_activeStrategy} to {newStrategy}");

        // Cleanup old strategy
        CleanupStrategy();

        // Set new strategy
        strategy = newStrategy;
        _activeStrategy = newStrategy;

        // Initialize new strategy
        InitializeStrategy();
    }

    public void StartMonitoring()
    {
        // Browser API doesn't need monitoring (event-driven)
        if (_activeStrategy == CheckingStrategy.BrowserAPI)
        {
            Log("Browser API: Event-driven, monitoring not needed");
            return;
        }

        if (_monitoringCoroutine == null)
        {
            _monitoringCoroutine = StartCoroutine(MonitorConnection());
            Log($"Started continuous monitoring (interval: {checkInterval}s)");
        }
    }

    public void StopMonitoring()
    {
        if (_monitoringCoroutine != null)
        {
            StopCoroutine(_monitoringCoroutine);
            _monitoringCoroutine = null;
            Log("Stopped continuous monitoring");
        }
    }

    public void CheckConnectionWithUrl(string url, Action<bool> callback)
    {
        StartCoroutine(CheckConnectionRoutine(url, callback));
    }

    /// <summary>
    /// Add a single URL to existing check URLs (no duplicates)
    /// </summary>
    public void AddCheckUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log("AddCheckUrl failed: URL is empty");
            return;
        }

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            Log($"AddCheckUrl failed: Invalid URL → {url}");
            return;
        }

        var list = new System.Collections.Generic.List<string>(checkUrls);

        if (list.Contains(url))
        {
            Log($"URL already exists: {url}");
            return;
        }

        list.Add(url);
        checkUrls = list.ToArray();

        Log($"Added check URL: {url}");
    }

    /// <summary>
    /// Replace all check URLs with a new array
    /// </summary>
    public void ReplaceCheckUrls(string[] urls)
    {
        if (urls == null || urls.Length == 0)
        {
            Log("ReplaceCheckUrls failed: URL list is null or empty");
            return;
        }

        var validUrls = new System.Collections.Generic.List<string>();

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) continue;
            if (!validUrls.Contains(url))
                validUrls.Add(url);
        }

        if (validUrls.Count == 0)
        {
            Log("ReplaceCheckUrls failed: No valid URLs found");
            return;
        }

        checkUrls = validUrls.ToArray();

        Log($"Replaced check URLs. Total: {checkUrls.Length}");
    }

    #endregion Public Methods

    #region Strategy: Browser API

    private void CheckWithBrowserAPI()
    {
        if (!_isWebGL)
        {
            Log("Error: BrowserAPI not available on this platform");
            return;
        }

        bool browserOnline = IsOnline();
        LogDetailed($"Browser navigator.onLine: {browserOnline}");
        UpdateConnectionStatus(browserOnline);
    }

    public void OnBrowserOnline()
    {
        Log("Browser online event");
        UpdateConnectionStatus(true);
    }

    public void OnBrowserOffline()
    {
        Log("Browser offline event");
        UpdateConnectionStatus(false);
    }

    #endregion Strategy: Browser API

    #region Strategy: Unity API

    private void CheckWithUnityAPI()
    {
        NetworkReachability reachability = Application.internetReachability;
        bool isConnected = reachability != NetworkReachability.NotReachable;

        LogDetailed($"Unity internetReachability: {reachability} → {(isConnected ? "Connected" : "Disconnected")}");
        UpdateConnectionStatus(isConnected);
    }

    #endregion Strategy: Unity API

    #region Strategy: HTTP

    private void CheckWithHTTP()
    {
        if (!_isChecking)
        {
            StartCoroutine(HTTPCheckRoutine());
        }
    }

    private IEnumerator HTTPCheckRoutine()
    {
        _isChecking = true;
        bool connectionResult = false;

        LogDetailed("Starting HTTP check...");

        foreach (string url in checkUrls)
        {
            yield return CheckConnectionRoutine(url, (result) =>
            {
                if (result)
                {
                    connectionResult = true;
                }
            });

            if (connectionResult)
            {
                break; // Exit early on success
            }
        }

        UpdateConnectionStatus(connectionResult);
        _isChecking = false;
    }

    #endregion Strategy: HTTP

    #region Strategy: Unity + HTTP

    private void CheckWithUnityAndHTTP()
    {
        if (!_isChecking)
        {
            StartCoroutine(UnityAndHTTPCheckRoutine());
        }
    }

    private IEnumerator UnityAndHTTPCheckRoutine()
    {
        _isChecking = true;

        // Stage 1: Unity API (instant)
        NetworkReachability reachability = Application.internetReachability;

        if (reachability == NetworkReachability.NotReachable)
        {
            LogDetailed("Stage 1 (Unity): No network detected");
            UpdateConnectionStatus(false);
            _isChecking = false;
            yield break;
        }

        LogDetailed($"Stage 1 (Unity): Network detected ({reachability})");
        LogDetailed("Stage 2 (HTTP): Verifying internet...");

        // Stage 2: HTTP verification
        bool connectionResult = false;

        foreach (string url in checkUrls)
        {
            yield return CheckConnectionRoutine(url, (result) =>
            {
                if (result)
                {
                    connectionResult = true;
                }
            });

            if (connectionResult)
            {
                break;
            }
        }

        UpdateConnectionStatus(connectionResult);
        _isChecking = false;
    }

    #endregion Strategy: Unity + HTTP

    #region Strategy: Browser + HTTP

    private void CheckWithBrowserAndHTTP()
    {
        if (!_isWebGL)
        {
            Log("Error: BrowserAndHTTP not available on this platform");
            return;
        }

        if (!_isChecking)
        {
            StartCoroutine(BrowserAndHTTPCheckRoutine());
        }
    }

    private IEnumerator BrowserAndHTTPCheckRoutine()
    {
        _isChecking = true;

        // Stage 1: Browser API (instant)
        bool browserOnline = IsOnline();

        if (!browserOnline)
        {
            LogDetailed("Stage 1 (Browser): Offline");
            UpdateConnectionStatus(false);
            _isChecking = false;
            yield break;
        }

        LogDetailed("Stage 1 (Browser): Online");
        LogDetailed("Stage 2 (HTTP): Verifying...");

        // Stage 2: HTTP verification
        bool verified = false;

        yield return CheckConnectionRoutine(checkUrls[0], (result) =>
        {
            verified = result;
        });

        UpdateConnectionStatus(verified);
        _isChecking = false;
    }

    public void OnBrowserOnlineWithVerification()
    {
        Log("Browser online event (verifying...)");
        CheckWithBrowserAndHTTP();
    }

    #endregion Strategy: Browser + HTTP

    #region HTTP Utilities

    private IEnumerator CheckConnectionRoutine(string url, Action<bool> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Head(url))
        {
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            bool isConnected = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                LogDetailed($"✓ HTTP success: {url}");
            }
            else
            {
                LogDetailed($"✗ HTTP failed: {url} ({request.error})");
            }

            callback?.Invoke(isConnected);
        }
    }

    private IEnumerator MonitorConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (!_isChecking)
            {
                CheckInternetConnection();
            }
        }
    }

    #endregion HTTP Utilities

    #region Status Management

    private void UpdateConnectionStatus(bool newStatus)
    {
        if (_isConnected != newStatus || !_hasInitialized)
        {
            _isConnected = newStatus;

            if (_isConnected)
            {
                Log("✅ Internet Connected");
                OnInternetConnected?.Invoke();
            }
            else
            {
                Log("❌ Internet Disconnected");
                OnInternetDisconnected?.Invoke();
            }

            OnConnectionStatusChanged?.Invoke(_isConnected);

            if (!_hasInitialized)
                _hasInitialized = true;
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FlexibleInternetManager] {message}");
        }
    }

    private void LogDetailed(string message)
    {
        if (showDebugLogs && showDetailedLogs)
        {
            Debug.Log($"[FlexibleInternetManager] {message}");
        }
    }

    #endregion Status Management

    #region Utility Methods

    public NetworkReachability GetUnityReachability()
    {
        return Application.internetReachability;
    }

    public bool GetBrowserOnlineStatus()
    {
        return _isWebGL ? IsOnline() : false;
    }

    public string GetConnectionType()
    {
        NetworkReachability reachability = Application.internetReachability;

        switch (reachability)
        {
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                return "WiFi";

            case NetworkReachability.ReachableViaCarrierDataNetwork:
                return "Mobile Data";

            default:
                return "None";
        }
    }

    public bool IsMeteredConnection()
    {
        return Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
    }

    #endregion Utility Methods
}