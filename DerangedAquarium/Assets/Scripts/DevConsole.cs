using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; // Required namespace to handle duplicate UI engines

public class DevConsole : MonoBehaviour
{
    [Header("UI Canvas References")]
    public GameObject consoleCanvasWindow;    
    public TMP_InputField commandInputField;  

    [Header("Console Toggle Hotkey")]
    public KeyCode toggleKey = KeyCode.BackQuote; 

    [Header("Registry of Spawnables")]
    public List<GameObject> spawnablePrefabs = new List<GameObject>();

    private bool isConsoleOpen = false;
    private bool wasCursorUnlockedBeforeOpening = false;

    private Dictionary<string, DevCommandBase> commandRegistry = new Dictionary<string, DevCommandBase>();

    private DevCommand<int> addMoneyCommand;
    private DevCommand<string> spawnPrefabCommand;
    private DevCommand helpCommand;

    void Awake()
    {
        // 1. The Money Code Command
        addMoneyCommand = new DevCommand<int>("money", "Adds money directly to the aquarium balance wallet.", "money <amount>", (amount) =>
        {
            AquariumManager manager = FindFirstObjectByType<AquariumManager>();
            if (manager != null)
            {
                manager.totalMoney += amount; 
                Debug.Log($"<color=green>[DevConsole]</color> Successfully added ${amount} to local wallet.");
            }
            else
            {
                Debug.LogWarning("[DevConsole] Cannot add money: AquariumManager could not be located inside active scenes!");
            }
        });

        // 2. The Prefab Spawner Command
        spawnPrefabCommand = new DevCommand<string>("spawn", "Spawns a registered asset prefab at coordinates (0,0).", "spawn <prefabName>", (prefabName) =>
        {
            GameObject targetPrefab = spawnablePrefabs.Find(p => p != null && p.name.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
            
            if (targetPrefab != null)
            {
                AquariumManager manager = FindFirstObjectByType<AquariumManager>();
                if (manager != null)
                {
                    if (targetPrefab.GetComponent<NaturalFishAI>() != null)
                    {
                        manager.SpawnBabyFish(targetPrefab, Vector3.zero);
                    }
                    else
                    {
                        Instantiate(targetPrefab, Vector3.zero, Quaternion.identity, manager.transform);
                    }
                    Debug.Log($"<color=cyan>[DevConsole]</color> Spawned asset: {targetPrefab.name}");
                }
                else
                {
                    Debug.LogWarning("[DevConsole] Cannot spawn asset: AquariumManager could not be located inside active scenes!");
                }
            }
            else
            {
                Debug.LogWarning($"[DevConsole] Prefab '{prefabName}' is missing from the DevConsole inspector array list!");
            }
        });

        // 3. The Help Summary Command
        helpCommand = new DevCommand("help", "Displays formatting instructions for all registered commands.", "help", () =>
        {
            Debug.Log("<color=yellow>--- CODES REGISTRY HELP LIST ---</color>");
            foreach (var command in commandRegistry.Values)
            {
                Debug.Log($"<b>{command.CommandFormat}</b> — {command.CommandDescription}");
            }
        });

        RegisterCommand(addMoneyCommand);
        RegisterCommand(spawnPrefabCommand);
        RegisterCommand(helpCommand);
    }

    void Start()
    {
        if (consoleCanvasWindow != null) consoleCanvasWindow.SetActive(false);
        if (commandInputField != null)
        {
            commandInputField.onSubmit.AddListener(OnSubmitCommand);
        }

        // --- AUTOMATIC TWIN EVENT SYSTEM CLEANUP ---
        // When the 2D Aquarium scene is loaded additively alongside your 3D Scene,
        // Unity ends up with 2 active EventSystems (one from each canvas environment).
        // This automatically finds duplicates, keeps the primary one active, and wipes the extra
        // to completely eliminate the duplicate EventSystem warning logs.
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Destroy(eventSystems[i].gameObject);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }
    }

    public void RegisterCommand(DevCommandBase command)
    {
        if (!commandRegistry.ContainsKey(command.CommandId))
        {
            commandRegistry.Add(command.CommandId, command);
        }
    }

    private void ToggleConsole()
    {
        isConsoleOpen = !isConsoleOpen;
        
        if (consoleCanvasWindow != null) consoleCanvasWindow.SetActive(isConsoleOpen);

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();

        if (isConsoleOpen)
        {
            wasCursorUnlockedBeforeOpening = (Cursor.lockState == CursorLockMode.None);

            if (player != null)
            {
                player.SetPlayerLockState(true); 
            }

            if (commandInputField != null)
            {
                commandInputField.Select();
                commandInputField.ActivateInputField();
                commandInputField.text = "";
            }
        }
        else
        {
            if (player != null)
            {
                player.SetPlayerLockState(wasCursorUnlockedBeforeOpening);
            }
            else
            {
                Cursor.lockState = wasCursorUnlockedBeforeOpening ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = !wasCursorUnlockedBeforeOpening;
            }
        }
    }

    private void OnSubmitCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        string[] splitInput = input.Split(' ');
        if (splitInput.Length == 0) return;

        string commandId = splitInput[0].ToLower();

        if (commandRegistry.ContainsKey(commandId))
        {
            DevCommandBase baseCommand = commandRegistry[commandId];

            if (baseCommand is DevCommand command)
            {
                command.Invoke();
            }
            else if (baseCommand is DevCommand<int> intCommand)
            {
                if (splitInput.Length > 1 && int.TryParse(splitInput[1], out int intArg))
                {
                    intCommand.Invoke(intArg);
                }
                else
                {
                    Debug.LogWarning($"[DevConsole] Typing formatting error. Usage: {baseCommand.CommandFormat}");
                }
            }
            else if (baseCommand is DevCommand<string> stringCommand)
            {
                if (splitInput.Length > 1)
                {
                    stringCommand.Invoke(splitInput[1]);
                }
                else
                {
                    Debug.LogWarning($"[DevConsole] Typing formatting error. Usage: {baseCommand.CommandFormat}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[DevConsole] Code prefix '{commandId}' not found. Type 'help' to review syntax.");
        }

        if (commandInputField != null)
        {
            commandInputField.text = "";
            commandInputField.ActivateInputField();
        }
    }
}