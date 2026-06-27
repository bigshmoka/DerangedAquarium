using UnityEngine;
using TMPro;
using System.Collections.Generic;

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

    // --- AUTOCOMPLETE STORAGE FIELDS ---
    private TMP_Text ghostTextMesh;
    private string currentSuggestion = "";

    // --- DYNAMIC COMMAND HISTORY FIELDS ---
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    // --- DYNAMIC RUNTIME INVENTORY CACHING ENGINES ---
    private List<string> autocompleteList = new List<string>();
    private Dictionary<string, GameObject> fishPrefabCache = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> storefrontPrefabCache = new Dictionary<string, GameObject>();

    // --- 2D MOUSE SELECTION DELETE CONTEXT MODE ---
    private bool isDevDeleting2D = false;

    void Start()
    {
        if (consolePanel != null) consolePanel.SetActive(false);
        
        if (commandInputField != null) 
        {
            commandInputField.DeactivateInputField();
            commandInputField.onSubmit.RemoveAllListeners();
            commandInputField.onSubmit.AddListener(ProcessSubmittedText);

            commandInputField.onValueChanged.RemoveAllListeners();
            commandInputField.onValueChanged.AddListener(OnInputValueChanged);

            InitializeBaseCommands();
            ScanAndCacheAllGamePrefabs();
            
            // SORT THE LIST ALPHABETICALLY TO PRIORITIZE ROOT WORDS OVER EXTENSIONS
            autocompleteList.Sort();
            
            CreateGhostTextOverlay();
        }

        if (logDisplayText != null) logDisplayText.text = "";

        // Shield the 3D player camera view from rendering 2D elements
        ApplyCameraCullingMasks();
    }

    void OnEnable()
    {
        Application.logMessageReceived += CaptureGameEngineLogs;
        Canvas.willRenderCanvases += ClampScrollbar;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= CaptureGameEngineLogs;
        Canvas.willRenderCanvases -= ClampScrollbar;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }

        // Deletion logic runs outside the console open block so it works when panel is hidden
        if (!isConsoleOpen && isDevDeleting2D)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                isDevDeleting2D = false;
                Debug.Log("<color=yellow>[Console Delete Mode]</color> 2D Tank Delete Mode Deactivated.");
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Camera aquariumCam = GetOrthographicAquariumCamera();
                
                Vector3 worldMousePos = aquariumCam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 targetPoint2D = new Vector2(worldMousePos.x, worldMousePos.y);
                
                SpriteRenderer[] allSceneSprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                
                float maxClickRadiusCheck = 1.5f; 
                float closestDistanceFound = maxClickRadiusCheck;
                Transform rootTargetToDestroy = null;

                foreach (SpriteRenderer sprite in allSceneSprites)
                {
                    if (sprite.transform.IsChildOf(this.transform) || sprite.gameObject.layer == LayerMask.NameToLayer("UI"))
                        continue;

                    float currentDistance = Vector2.Distance(targetPoint2D, sprite.transform.position);

                    if (currentDistance < closestDistanceFound)
                    {
                        Transform currentCheckNode = sprite.transform;
                        while (currentCheckNode.parent != null && 
                               currentCheckNode.parent.GetComponent<AquariumManager>() == null && 
                               !currentCheckNode.parent.name.Contains("---"))
                        {
                            currentCheckNode = currentCheckNode.parent;
                        }

                        string spriteNameLower = sprite.gameObject.name.ToLower();
                        string rootNameLower = currentCheckNode.gameObject.name.ToLower();

                        bool isAlgaeGrid = currentCheckNode.GetComponent<AlgaeNode>() != null || rootNameLower.Contains("algae");
                        bool isCoreEngine = rootNameLower.Contains("manager") || rootNameLower.Contains("camera") || rootNameLower.Contains("canvas");
                        
                        bool isBackgroundPrefab = spriteNameLower.Contains("background") || rootNameLower.Contains("background") ||
                                                  spriteNameLower.Contains("backdrop")   || rootNameLower.Contains("backdrop")   ||
                                                  spriteNameLower.Contains("glass")      || rootNameLower.Contains("glass")      ||
                                                  spriteNameLower.Contains("grid")       || rootNameLower.Contains("grid")       ||
                                                  spriteNameLower.Contains("wall")       || rootNameLower.Contains("wall");

                        if (isAlgaeGrid || isCoreEngine || isBackgroundPrefab)
                        {
                            continue; 
                        }

                        closestDistanceFound = currentDistance;
                        rootTargetToDestroy = currentCheckNode;
                    }
                }

                if (rootTargetToDestroy != null)
                {
                    Debug.Log($"<color=red>[Console Delete Mode]</color> Vaporized 2D Object Target Asset: <b>{rootTargetToDestroy.gameObject.name}</b>");
                    Destroy(rootTargetToDestroy.gameObject);
                }
            }
        }

        if (!isConsoleOpen) return;

        // PHYSICAL TAB AUTOCOMPLETE WITH ARGUMENT SPACING
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!string.IsNullOrEmpty(currentSuggestion))
            {
                string filledCommand = currentSuggestion;

                if (filledCommand == "money" || filledCommand == "spawn" || filledCommand == "timescale")
                {
                    filledCommand += " ";
                }

                commandInputField.text = filledCommand;
                commandInputField.caretPosition = filledCommand.Length;
                commandInputField.ActivateInputField();

                currentSuggestion = "";
                OnInputValueChanged(filledCommand);
            }
        }

        // CLEAN ERROR-FREE COMMAND HISTORY RECALL
        if (Input.GetKeyDown(KeyCode.UpArrow) && commandHistory.Count > 0)
        {
            historyIndex--;
            if (historyIndex < 0) historyIndex = 0; 

            commandInputField.text = commandHistory[historyIndex];
            commandInputField.caretPosition = commandInputField.text.Length;
            commandInputField.ActivateInputField();
        }

        // DOWN ARROW HISTORY NAVIGATION FORWARD
        if (Input.GetKeyDown(KeyCode.DownArrow) && commandHistory.Count > 0)
        {
            historyIndex++;
            if (historyIndex >= commandHistory.Count)
            {
                historyIndex = commandHistory.Count;
                commandInputField.text = ""; 
            }
            else
            {
                commandInputField.text = commandHistory[historyIndex];
            }
            
            commandInputField.caretPosition = commandInputField.text.Length;
            commandInputField.ActivateInputField();
        }
    }

    private void InitializeBaseCommands()
    {
        autocompleteList.Clear();
        autocompleteList.Add("help");
        autocompleteList.Add("noclip");
        autocompleteList.Add("timescale");
        autocompleteList.Add("clearalgae");
        autocompleteList.Add("growalgae");
        autocompleteList.Add("growlagae");
        autocompleteList.Add("money");
        autocompleteList.Add("clearitems");
        autocompleteList.Add("save");
        autocompleteList.Add("load");
        autocompleteList.Add("spawn");
        autocompleteList.Add("delete");
        autocompleteList.Add("clear");
        autocompleteList.Add("cls");
        
        // --- INTEGRATED: ADD NEW IN-GAME SYSTEM HOOKS ---
        autocompleteList.Add("quests");
        autocompleteList.Add("skipquest");
    }

    private void ScanAndCacheAllGamePrefabs()
    {
        fishPrefabCache.Clear();
        storefrontPrefabCache.Clear();

        GameObject[] loadedAquaticAssets = Resources.LoadAll<GameObject>("AquariumPrefabs");
        foreach (GameObject prefab in loadedAquaticAssets)
        {
            if (prefab != null)
            {
                string lowerCaseName = prefab.name.ToLower();
                autocompleteList.Add("spawn " + lowerCaseName);
                if (!fishPrefabCache.ContainsKey(lowerCaseName))
                {
                    fishPrefabCache.Add(lowerCaseName, prefab);
                }
            }
        }

        GameObject[] loadedFurnitureAssets = Resources.LoadAll<GameObject>("StorefrontPrefabs");
        foreach (GameObject prefab in loadedFurnitureAssets)
        {
            if (prefab != null)
            {
                string lowerCaseName = prefab.name.ToLower();
                autocompleteList.Add("spawn " + lowerCaseName);
                if (!storefrontPrefabCache.ContainsKey(lowerCaseName))
                {
                    storefrontPrefabCache.Add(lowerCaseName, prefab);
                }
            }
        }
    }

    private void ClampScrollbar()
    {
        if (isConsoleOpen && logScrollRect != null && logScrollRect.verticalScrollbar != null)
        {
            if (logScrollRect.verticalScrollbar.size < 0.05f)
            {
                logScrollRect.verticalScrollbar.size = 0.05f;
            }
        }
    }

    private void CreateGhostTextOverlay()
    {
        if (commandInputField == null) return;

        TMP_Text mainTextComponent = commandInputField.textComponent;
        if (mainTextComponent == null) return;

        GameObject ghostObj = new GameObject("ConsoleGhostPreviewText");
        ghostObj.transform.SetParent(mainTextComponent.transform.parent, false);

        ghostTextMesh = ghostObj.AddComponent<TextMeshProUGUI>();
        ghostTextMesh.font = mainTextComponent.font;
        ghostTextMesh.fontSize = mainTextComponent.fontSize;
        ghostTextMesh.fontStyle = mainTextComponent.fontStyle;
        ghostTextMesh.alignment = mainTextComponent.alignment;
        ghostTextMesh.margin = mainTextComponent.margin;

        ghostTextMesh.color = new Color(1f, 1f, 1f, 0.35f); 
        ghostTextMesh.raycastTarget = false; 

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        RectTransform mainRect = mainTextComponent.GetComponent<RectTransform>();
        
        if (ghostRect != null && mainRect != null)
        {
            ghostRect.anchorMin = mainRect.anchorMin;
            ghostRect.anchorMax = mainRect.anchorMax;
            ghostRect.pivot = mainRect.pivot;
            ghostRect.anchoredPosition = mainRect.anchoredPosition;
            ghostRect.sizeDelta = mainRect.sizeDelta;
        }

        ghostObj.transform.SetAsFirstSibling();
        ghostTextMesh.text = "";
    }

    private void OnInputValueChanged(string currentInput)
    {
        currentSuggestion = "";
        if (ghostTextMesh != null) ghostTextMesh.text = "";

        if (string.IsNullOrEmpty(currentInput) || !isConsoleOpen)
        {
            return;
        }

        string lowerInput = currentInput.ToLower();

        if (lowerInput == "money" || lowerInput == "money ")
        {
            string spacingBuffer = lowerInput == "money" ? " " : "";
            if (ghostTextMesh != null) ghostTextMesh.text = currentInput + spacingBuffer + "<color=#55FF55>[amount]</color>";
            return;
        }
        if (lowerInput == "spawn" || lowerInput == "spawn ")
        {
            string spacingBuffer = lowerInput == "spawn" ? " " : "";
            if (ghostTextMesh != null) ghostTextMesh.text = currentInput + spacingBuffer + "<color=#33CCFF>[asset_name]</color>";
            return;
        }
        if (lowerInput == "timescale" || lowerInput == "timescale ")
        {
            string spacingBuffer = lowerInput == "timescale" ? " " : "";
            if (ghostTextMesh != null) ghostTextMesh.text = currentInput + spacingBuffer + "<color=#FFCC00>[multiplier]</color>";
            return;
        }

        foreach (string command in autocompleteList)
        {
            if (command.StartsWith(lowerInput))
            {
                currentSuggestion = command;
                break;
            }
        }

        if (!string.IsNullOrEmpty(currentSuggestion) && ghostTextMesh != null)
        {
            if (currentSuggestion.Length > currentInput.Length)
            {
                string hiddenSuffix = currentSuggestion.Substring(currentInput.Length);
                ghostTextMesh.text = currentInput + hiddenSuffix;
            }
            else
            {
                ghostTextMesh.text = ""; 
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
            isDevDeleting2D = false;

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

            if (ghostTextMesh != null) ghostTextMesh.text = "";
            currentSuggestion = "";

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

        // --- RECURSION SHEILD: DROPS TEXT GLYPH ERRORS IMMEDIATELY ---
        if (logMessage.Contains("font asset") || logMessage.Contains("Unicode value") || logMessage.Contains("character with Unicode"))
        {
            return;
        }

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

        if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != cleanedInput)
        {
            commandHistory.Add(cleanedInput);
        }
        
        historyIndex = commandHistory.Count;

        string[] inputPieces = cleanedInput.Split(' ');
        if (inputPieces.Length == 0) return;

        string mainCommand = inputPieces[0].ToLower();
        
        string commandArguments = "";
        if (inputPieces.Length > 1)
        {
            commandArguments = cleanedInput.Substring(mainCommand.Length).Trim();
        }

        switch (mainCommand)
        {
            case "help":
                ExecuteHelpCommand();
                break;

            case "clear":
            case "cls":
                if (logDisplayText != null) logDisplayText.text = "";
                Debug.Log("[Console] Log screen cleared successfully.");
                break;

            case "quests":
                ExecuteQuestsCommand();
                break;

            case "skipquest":
                ExecuteSkipQuestCommand();
                break;

            case "noclip":
                ExecuteNoclipCommand();
                break;

            case "timescale":
                ExecuteTimescaleCommand(commandArguments);
                break;

            case "clearalgae":
                ExecuteClearAlgaeCommand();
                break;

            case "growalgae":
            case "growlagae": 
                ExecuteGrowAlgaeCommand();
                break;

            case "spawn":
                ExecuteSpawnCommand(commandArguments);
                break;

            case "delete":
                ExecuteDeleteCommand();
                break;

            case "money":
                ExecuteMoneyCheat(commandArguments);
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

        if (ghostTextMesh != null) ghostTextMesh.text = "";
        currentSuggestion = "";

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
                  "• <b>clear / cls</b> - Instantly wipes all previous console messages and clears the view.\n" +
                  "• <b>quests</b> - Displays your currently active quest progression registry cleanly in the logs.\n" +
                  "• <b>skipquest</b> - Forces the current quest to be skipped to the next one in line.\n" +
                  "• <b>noclip</b> - Toggles fly mode to pass through wall meshes and move out-of-bounds.\n" +
                  "• <b>timescale <float></b> - Adjusts simulation flow speed (e.g., 'timescale 4' speeds up growth and algae cycles).\n" +
                  "• <b>clearalgae</b> - Instantly clears away all green algae from every window node pane in the tank.\n" +
                  "• <b>growalgae</b> - Forces every live fish currently swimming in the tank to expand its scale values.\n" +
                  "• <b>spawn <asset_name></b> - Dynamic spawner matching exact filenames in both 2D Aquarium and 3D Storefront folders. 3D items activate ghost previews!\n" +
                  "• <b>delete</b> - Smart target clear mode. Opens click-to-delete context tool for 2D tank items or triggers 3D Removal Mode system.\n" +
                  "• <b>money <integer></b> - Adds cash into the centralized economy manager wallet tracking balance.\n" +
                  "• <b>clearitems</b> - Instantly sweeps and deletes all placed 3D shop furniture elements.\n" +
                  "• <b>save</b> - Commits finances, shop layouts, fish growth metrics, and algae nodes to local file.\n" +
                  "• <b>load</b> - Completely rebuilds your multi-scene game status using your persistent file registry.");
    }

    private void ExecuteQuestsCommand()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[Console] QuestManager instance cannot be tracked in current memory runtime.");
            return;
        }

        // OUTPUT TIMER FORMATTING REMAINING TIME VALUES TO THE LOG VIEW WINDOW LAYER
        Debug.Log($"<b>=== ACTIVE TYCOON QUEST REGISTRY (Time Left: {QuestManager.Instance.GetTimeRemainingString()}) ===</b>");
        foreach (Quest q in QuestManager.Instance.activeQuests)
        {
            string markerState = q.isCompleted 
                ? "<color=#55FF55>[COMPLETED]</color>" 
                : $"<color=#FFCC00>({q.currentCount}/{q.targetCount})</color>";

            Debug.Log($"• {q.description} - {markerState} | Reward: <color=#66FF66>${q.cashReward}</color>");
        }
    }

    // --- NEW: THE INSTANT TESTING HARNESS EVENT SIMULATOR ---
    private void ExecuteSkipQuestCommand()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[Console] QuestManager instance cannot be tracked in current memory runtime.");
            return;
        }

        QuestManager.Instance.RotateDailyQuests();
        Debug.Log("<color=cyan>[Console Tooling]</color> Successfully bypassed timer constraints! Accelerated 24 hours into the future.");
    }

    private void ExecuteDeleteCommand()
    {
        AquariumManager aquariumManager = FindFirstObjectByType<AquariumManager>();
        bool isViewingAquarium = (aquariumManager != null && aquariumManager.isTankVisible);

        if (isViewingAquarium)
        {
            ToggleConsole();
            isDevDeleting2D = true;
            Debug.Log("<color=yellow>[Console Delete Mode]</color> 2D Aquarium Delete Active! Left-Click any fish, snail, decoration, or feeder to delete it. Algae panels and structural backgrounds are fully protected. Press Escape or Right-Click to leave.");
        }
        else
        {
            StorefrontRemovalSystem removalSystem = FindFirstObjectByType<StorefrontRemovalSystem>();
            if (removalSystem != null)
            {
                ToggleConsole();
                removalSystem.StartRemovalMode();
            }
            else
            {
                Debug.LogWarning("[Console] StorefrontRemovalSystem component could not be tracked in current scene contexts.");
            }
        }
    }

    private void ExecuteSpawnCommand(string argument)
    {
        string cleanedName = argument.Trim().ToLower();

        if (string.IsNullOrEmpty(cleanedName))
        {
            Debug.LogWarning("[Console] Spawn command requires an asset name (e.g., 'spawn shelf' or 'spawn snail').");
            return;
        }

        if (fishPrefabCache.TryGetValue(cleanedName, out GameObject target2DPrefab))
        {
            AquariumManager currentActiveTankManager = FindFirstObjectByType<AquariumManager>();
            if (currentActiveTankManager != null)
            {
                Vector3 localizedSpawnPoint = currentActiveTankManager.transform.position;
                
                currentActiveTankManager.SpawnBabyFish(target2DPrefab, localizedSpawnPoint);

                int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
                if (aqLayerIndex != -1)
                {
                    foreach (NaturalFishAI fish in FindObjectsByType<NaturalFishAI>(FindObjectsSortMode.None))
                    {
                        if (fish.gameObject.layer != aqLayerIndex)
                        {
                            SetLayerRecursive(fish.gameObject, aqLayerIndex);
                        }
                    }
                    foreach (SnailAI snail in FindObjectsByType<SnailAI>(FindObjectsSortMode.None))
                    {
                        if (snail.gameObject.layer != aqLayerIndex)
                        {
                            SetLayerRecursive(snail.gameObject, aqLayerIndex);
                        }
                    }
                    foreach (SpriteRenderer sprite in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    {
                        if (sprite.transform.IsChildOf(currentActiveTankManager.transform) && sprite.gameObject.layer != aqLayerIndex)
                        {
                            SetLayerRecursive(sprite.gameObject, aqLayerIndex);
                        }
                    }
                }

                Debug.Log($"<color=cyan>[Console]</color> Successfully spawned custom fish type instance: <b>{target2DPrefab.name}</b> at native scale sizes.");
            }
            else
            {
                Debug.LogWarning("[Console] No active AquariumManager found to handle 2D assets.");
            }
            return;
        }

        if (storefrontPrefabCache.TryGetValue(cleanedName, out GameObject target3DPrefab))
        {
            StorefrontPlacementSystem placementSystem = FindFirstObjectByType<StorefrontPlacementSystem>();
            if (placementSystem != null)
            {
                ToggleConsole();
                placementSystem.StartPlacement(target3DPrefab, 0);
                Debug.Log($"<color=cyan>[Console]</color> Initiated 3D cheat placement mode for: <b>{target3DPrefab.name}</b> ($0).");
            }
            else
            {
                Debug.LogWarning("[Console] No active StorefrontPlacementSystem found to handle 3D objects.");
            }
            return;
        }

        Debug.LogError($"[Console] Spawn Failed. No prefab named '<b>{argument}</b>' found in AquariumPrefabs or StorefrontPrefabs directories.");
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void ApplyCameraCullingMasks()
    {
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null && player.playerCamera != null)
        {
            Camera main3DCameraComponent = player.playerCamera.GetComponent<Camera>();
            if (main3DCameraComponent != null)
            {
                int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
                if (aqLayerIndex != -1)
                {
                    main3DCameraComponent.cullingMask &= ~(1 << aqLayerIndex);
                }
            }
        }
    }

    private Camera GetOrthographicAquariumCamera()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.orthographic)
            {
                if (aqLayerIndex != -1 && (cam.cullingMask & (1 << aqLayerIndex)) != 0)
                {
                    return cam;
                }
            }
        }

        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.orthographic && (cam.name.Contains("Aquarium") || cam.name.Contains("Tank"))) 
                return cam;
        }

        return Camera.main; 
    }

    private void ExecuteTimescaleCommand(string argument)
    {
        if (float.TryParse(argument, out float scaleAmt))
        {
            Time.timeScale = Mathf.Clamp(scaleAmt, 0f, 100f);
            Debug.Log($"[Console] Simulation speed set to: <b>{Time.timeScale}x normal speed</b>.");
        }
        else
        {
            Debug.LogWarning("[Console] Invalid entry. Format syntax requires a numeric value (e.g., 'timescale 3.5').");
        }
    }

    private void ExecuteClearAlgaeCommand()
    {
        AlgaeManager algaeManager = FindFirstObjectByType<AlgaeManager>();
        if (algaeManager != null && algaeManager.algaeNodes != null)
        {
            int nodesWiped = 0;
            foreach (AlgaeNode node in algaeManager.algaeNodes)
            {
                if (node != null)
                {
                    node.InitializeAlgaeLevel(0f);
                    nodesWiped++;
                }
            }
            Debug.Log($"<color=green>[Console]</color> Glass sanitized! Cleaned <b>{nodesWiped}</b> tank surface points.");
        }
        else
        {
            Debug.LogWarning("[Console] Aborted. AlgaeManager component sequence arrays could not be resolved.");
        }
    }

    private void ExecuteGrowAlgaeCommand()
    {
        NaturalFishAI[] activeFish = FindObjectsByType<NaturalFishAI>(FindObjectsSortMode.None);
        if (activeFish != null && activeFish.Length > 0)
        {
            foreach (NaturalFishAI fish in activeFish)
            {
                if (fish != null)
                {
                    fish.currentScaleModifier += 0.25f;
                    if (fish.currentScaleModifier > fish.maxScale)
                    {
                        fish.currentScaleModifier = fish.maxScale;
                    }
                    fish.UpdateFishScale(); 
                }
            }
            Debug.Log($"<color=cyan>[Console]</color> Growth surge deployed. Boosted sizes across <b>{activeFish.Length}</b> active fish.");
        }
        else
        {
            Debug.LogWarning("[Console] Command skipped. No live fish instances found inside the tank boundaries.");
        }
    }

    private void ExecuteNoclipCommand()
    {
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null)
        {
            bool noclipActive = player.ToggleNoclip();
            Debug.Log($"[Console] Noclip flight mode: " + (noclipActive ? "<color=green>ACTIVE</color>" : "<color=red>INACTIVE</color>"));
        }
        else
        {
            Debug.LogWarning("[Console] Execution failed. 3D Player actor script could not be located.");
        }
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

    private void ExecuteClearStorefrontItemsCheat()
    {
        GameObject KaplanContainer = GameObject.Find("--- PLACED 3D ITEMS ---");
        if (KaplanContainer != null && KaplanContainer.transform.childCount > 0)
        {
            int structuralChildCount = KaplanContainer.transform.childCount;
            for (int i = structuralChildCount - 1; i >= 0; i--)
            {
                Destroy(KaplanContainer.transform.GetChild(i).gameObject);
            }
            Debug.Log($"[Janitor Sweep] Swept and wiped clean all {structuralChildCount} active placed items.");
        }
    }
}