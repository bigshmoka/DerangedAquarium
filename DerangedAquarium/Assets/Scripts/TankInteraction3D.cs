using UnityEngine;
using UnityEngine.SceneManagement;

public class TankInteraction3D : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The EXACT string name of your existing 2D Aquarium scene file.")]
    public string aquariumSceneName = "AquariumScene";

    [Header("UI Prompt")]
    public GameObject pressEPromptUI;

    private bool isPlayerNearby = false;
    private bool isViewingTank = false;
    private bool hasSceneBeenLoadedOnce = false;
    private PlayerController3D localPlayer;

    void Start()
    {
        if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
    }

    void Update()
    {
        // Case 1: Player is near the tank and hits 'E' to open the view
        if (isPlayerNearby && !isViewingTank && Input.GetKeyDown(KeyCode.E))
        {
            EnterAquariumView();
        }
        // Case 2: Player wants to step away into the 3D shop using 'Q'
        // Using 'Q' prevents the Unity Editor from stealing mouse focus!
        else if (isViewingTank && Input.GetKeyDown(KeyCode.Q))
        {
            ExitAquariumView();
        }
    }

    void EnterAquariumView()
    {
        isViewingTank = true;
        if (pressEPromptUI != null) pressEPromptUI.SetActive(false);

        if (localPlayer != null) localPlayer.SetPlayerLockState(true);
        if (Camera.main != null) Camera.main.gameObject.SetActive(false);

        if (!hasSceneBeenLoadedOnce)
        {
            SceneManager.LoadScene(aquariumSceneName, LoadSceneMode.Additive);
            hasSceneBeenLoadedOnce = true;
        }
        else
        {
            Toggle2DAquariumVisibility(true);
        }

        // UNLOCK MOUSE FOR TANK MENU NAVIGATING
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitAquariumView()
    {
        isViewingTank = false;

        if (localPlayer != null) localPlayer.SetPlayerLockState(false);

        // Hide the 2D art assets from the 3D camera view while keeping scripts running!
        Toggle2DAquariumVisibility(false);

        Camera playerCam = localPlayer.playerCamera.GetComponent<Camera>();
        if (playerCam != null) playerCam.gameObject.SetActive(true);

        if (isPlayerNearby && pressEPromptUI != null) pressEPromptUI.SetActive(true);

        // INSTANT FIRST-PERSON LOOK CAPTURE
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // This method hides the VISUALS but leaves the simulation logic running perfectly
    void Toggle2DAquariumVisibility(bool makeVisible)
    {
        Scene aquariumScene = SceneManager.GetSceneByName(aquariumSceneName);
        if (!aquariumScene.isLoaded) return;

        GameObject[] rootObjects = aquariumScene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            // 1. Turn off/on all SpriteRenderers and MeshRenderers 
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = makeVisible;
            }

            // 2. Turn off/on the User Interface Canvases
            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases)
            {
                c.enabled = makeVisible;
            }

            // 3. Turn off/on the 2D Camera component itself
            Camera cam = obj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.enabled = makeVisible;
            }
        }
    }

    // --- 3D TRIGGER DETECTIONS ---
    void OnTriggerEnter(Collider other)
    {
        // Prints out the name of ANY 3D object that walks into your tank zone
        Debug.Log($"[Tank Trigger] Something entered the zone: {other.gameObject.name}");

        PlayerController3D player = other.GetComponent<Collider>().GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();

        if (player != null)
        {
            Debug.Log("<color=green>[Tank Trigger]</color> SUCCESS: Found the PlayerController3D script!");
            isPlayerNearby = true;
            localPlayer = player;
            if (pressEPromptUI != null && !isViewingTank) pressEPromptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController3D player = other.GetComponent<Collider>().GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();

        if (player != null)
        {
            Debug.Log("<color=orange>[Tank Trigger]</color> Player walked away from the tank.");
            isPlayerNearby = false;
            localPlayer = null;
            if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        }
    }
}