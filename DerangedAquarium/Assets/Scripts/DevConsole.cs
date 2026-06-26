using UnityEngine;
using TMPro;

public class DevConsole : MonoBehaviour
{
    [Header("Console UI References")]
    public GameObject consolePanel;
    public TMP_InputField commandInputField;
    
    [Tooltip("Drag the TextMeshPro Text component located inside your ScrollView Content viewport layer here.")]
    public TMP_Text logDisplayText;

    [Header("Optional Scrolling Mechanics")]
    [Tooltip("Drag the main ConsoleLogScrollView GameObject container here to enable automated bottom-edge snapping hooks.")]
    public UnityEngine.UI.ScrollRect logScrollRect;

    [Header("Console Toggle Key")]
    public KeyCode toggleKey = KeyCode.BackQuote; // The tilde/backquote key (~)

    private bool isConsoleOpen = false;

    void Start()
    {
        if (consolePanel != null) consolePanel.SetActive(false);
        
        if (commandInputField != null) 
        {
            commandInputField.DeactivateInputField();
            commandInputField.onSubmit.RemoveAllListeners();
            commandInputField.onSubmit.AddListener(ProcessSubmittedText);
        }

        if (logDisplayText != null) logDisplayText.text = "";
    }

    void OnEnable()
    {
        Application.logMessageReceived += CaptureGameEngineLogs;
        
        // --- THE ULTIMATE SCROLLBAR FIX ---
        // We subscribe to the absolute last millisecond of Unity's UI rendering pipeline.
        Canvas.willRenderCanvases += ClampScrollbar;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= CaptureGameEngineLogs;
        
        // Always clean up the subscription to prevent memory leaks!
        Canvas.willRenderCanvases -= ClampScrollbar;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }
    }

    // --- NEW: THE RENDER-PASS CLAMP ---
    private void ClampScrollbar()
    {
        // Because this runs AFTER Unity's hidden ScrollRect math, we get the final word.
        // It allows the handle to shrink naturally as the log grows, but completely 
        // stops it from ever shrinking smaller than 5% (0.05f) of the window height.
        if (isConsoleOpen && logScrollRect != null && logScrollRect.verticalScrollbar != null)
        {
            if (logScrollRect.verticalScrollbar.size < 0.05f)
            {
                logScrollRect.verticalScrollbar.size = 0.05f;
            }
        }
    }

    public void ToggleConsole()
    {
        isConsoleOpen = !isConsoleOpen;

        if (consolePanel != null)
        {
            consolePanel.SetActive(isConsoleOpen);
        }

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();

        if (isConsoleOpen)
        {
            if (commandInputField != null)
            {
                commandInputField.ActivateInputField();
                commandInputField.text = ""; 
            }

            if (player != null) player.SetPlayerLockState(true);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            if (commandInputField != null) commandInputField.DeactivateInputField();

            StorefrontShopUI storefrontShop = FindFirstObjectByType<StorefrontShopUI>();
            AquariumManager aquariumManager = FindFirstObjectByType<AquariumManager>();

            if (player != null)
            {
                bool isStoreShopOpen = (storefrontShop != null && storefrontShop.isShopOpen);
                bool isViewingAquarium = (aquariumManager != null && aquariumManager.isTankVisible);
                
                bool shouldKeepMouseUnlocked = isStoreShopOpen || isViewingAquarium;
                player.SetPlayerLockState(shouldKeepMouseUnlocked);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void CaptureGameEngineLogs(string logMessage, string stackTrace, LogType logType)
    {
        if (logDisplayText == null) return;

        string textHexColor = "#DFDFDF"; 
        
        if (logType == LogType.Warning)
        {
            textHexColor = "#FFCC00"; 
        }
        else if (logType == LogType.Error || logType == LogType.Exception)
        {
            textHexColor = "#FF3333"; 
        }
        else if (logMessage.StartsWith("]"))
        {
            textHexColor = "#55FF55"; 
        }

        logDisplayText.text += $"<color={textHexColor}>{logMessage}</color>\n";

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f; 
        }
    }

    private void ProcessSubmittedText(string rawInput)
    {
        if (string.IsNullOrEmpty(rawInput)) return;

        string cleanedInput = rawInput.Trim();
        Debug.Log($"] {cleanedInput}");

        string[] inputPieces = cleanedInput.Split(' ');
        if (inputPieces.Length == 0) return;

        string mainCommand = inputPieces[0].ToLower();
        string commandArguments = inputPieces.Length > 1 ? inputPieces[1] : "";

        switch (mainCommand)
        {
            case "help":
                ExecuteHelpCommand();
                break;

            case "money":
                ExecuteMoneyCheat(commandArguments);
                break;

            case "spawnfish":
                ExecuteSpawnFishCheat();
                break;

            case "clearitems":
                ExecuteClearStorefrontItemsCheat();
                break;

            case "save":
                if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
                break;

            case "load":
                if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();
                break;

            default:
                Debug.LogWarning($"[Console] Unrecognized command code signature: '{mainCommand}'");
                break;
        }

        if (isConsoleOpen && commandInputField != null)
        {
            commandInputField.text = "";
            commandInputField.ActivateInputField();
        }
    }

    private void ExecuteHelpCommand()
    {
        Debug.Log("<b>=== DEV CONSOLE HELP REGISTRY ===</b>\n" +
                  "• <b>help</b> - Displays this active cheat command overview panel.\n" +
                  "• <b>money <integer></b> - Adds cash to the global economy wallet.\n" +
                  "• <b>spawnfish</b> - Instantiates a default test fish at the tank's center coordinates.\n" +
                  "• <b>clearitems</b> - Instantly destroys all placed 3D items inside your shop container.\n" +
                  "• <b>save</b> - Commits current finances, shop layouts, fish growth metrics, and algae nodes to file.\n" +
                  "• <b>load</b> - Fully rebuilds your game state using your persistent file registry.");
    }

    private void ExecuteMoneyCheat(string argument)
    {
        if (int.TryParse(argument, out int moneyAmt))
        {
            if (GlobalEconomyManager.Instance != null)
            {
                GlobalEconomyManager.Instance.AddMoney(moneyAmt);
                Debug.Log($"[Wallet Injection] Deposited +${moneyAmt} into global wallet ledger.");
            }
        }
    }

    private void ExecuteSpawnFishCheat()
    {
        AquariumManager currentActiveTankManager = FindFirstObjectByType<AquariumManager>();

        if (currentActiveTankManager != null && currentActiveTankManager.fishPrefab != null)
        {
            currentActiveTankManager.SpawnBabyFish(currentActiveTankManager.fishPrefab, Vector3.zero);
            Debug.Log("[Creature Spawner] Spawned extra cheat test fish directly at tank center origin point coordinates.");
        }
    }

    private void ExecuteClearStorefrontItemsCheat()
    {
        GameObject placedContainer = GameObject.Find("--- PLACED 3D ITEMS ---");

        if (placedContainer != null && placedContainer.transform.childCount > 0)
        {
            int structuralChildCount = placedContainer.transform.childCount;
            for (int i = structuralChildCount - 1; i >= 0; i--)
            {
                Destroy(placedContainer.transform.GetChild(i).gameObject);
            }
            Debug.Log($"[Janitor Sweep] Swept and wiped clean all {structuralChildCount} active placed items.");
        }
    }
}