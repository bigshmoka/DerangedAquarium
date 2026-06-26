using UnityEngine;
using TMPro;

public class DevConsole : MonoBehaviour
{
    [Header("Console UI References")]
    public GameObject consolePanel;
    public TMP_InputField commandInputField;

    [Header("Console Toggle Key")]
    public KeyCode toggleKey = KeyCode.BackQuote; 

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
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
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
            if (player != null)
            {
                bool shouldKeepMouseUnlocked = (storefrontShop != null && storefrontShop.isShopOpen);
                player.SetPlayerLockState(shouldKeepMouseUnlocked);
            }
        }
    }

    private void ProcessSubmittedText(string rawInput)
    {
        if (string.IsNullOrEmpty(rawInput)) return;

        string cleanedInput = rawInput.Trim();
        string[] inputPieces = cleanedInput.Split(' ');

        if (inputPieces.Length == 0) return;

        string mainCommand = inputPieces[0].ToLower();
        string commandArguments = inputPieces.Length > 1 ? inputPieces[1] : "";

        switch (mainCommand)
        {
            case "money":
                ExecuteMoneyCheat(commandArguments);
                break;

            case "spawnfish":
                ExecuteSpawnFishCheat();
                break;

            case "clearitems":
                ExecuteClearStorefrontItemsCheat();
                break;

            // --- NEW: SYSTEM STORAGE CONSOLE CODES ---
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

    private void ExecuteMoneyCheat(string argument)
    {
        if (int.TryParse(argument, out int moneyAmt))
        {
            if (GlobalEconomyManager.Instance != null)
            {
                GlobalEconomyManager.Instance.AddMoney(moneyAmt);
                Debug.Log($"<color=green>[Console] Cheat Success:</color> Deposited +${moneyAmt} into global wallet ledger.");
            }
        }
    }

    private void ExecuteSpawnFishCheat()
    {
        AquariumManager currentActiveTankManager = FindFirstObjectByType<AquariumManager>();

        if (currentActiveTankManager != null && currentActiveTankManager.fishPrefab != null)
        {
            currentActiveTankManager.SpawnBabyFish(currentActiveTankManager.fishPrefab, Vector3.zero);
            Debug.Log("<color=cyan>[Console]</color> Spawned extra cheat test fish directly at tank center origin point coordinates.");
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
            Debug.Log($"<color=red>[Console]</color> Swept and wiped clean all {structuralChildCount} active placed items.");
        }
    }
}