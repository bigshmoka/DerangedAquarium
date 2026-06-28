using UnityEngine;
using UnityEngine.SceneManagement;

public class TankInteraction3D : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string aquariumSceneName = "AquariumScene";

    [Header("Tank Multi-Instance Tracking")]
    public string tankID = "StarterTank";

    [Header("UI Prompt")]
    public GameObject pressEPromptUI;

    private bool isPlayerNearby = false;
    private bool isViewingTank = false;
    private PlayerController3D localPlayer;
    
    private AquariumManager myPaired2DManager;
    private static bool isSceneLoading = false;

    void Start()
    {
        if (pressEPromptUI != null) pressEPromptUI.SetActive(false);

        Scene aquariumScene = SceneManager.GetSceneByName(aquariumSceneName);
        
        if (!aquariumScene.isLoaded && !isSceneLoading)
        {
            isSceneLoading = true;
            SceneManager.LoadScene(aquariumSceneName, LoadSceneMode.Additive);
            StartCoroutine(InitializeTankVisibilityOnStart());
        }
        else
        {
            StartCoroutine(WaitForSceneAndLink());
        }
    }

    private System.Collections.IEnumerator InitializeTankVisibilityOnStart()
    {
        Scene aquariumScene = SceneManager.GetSceneByName(aquariumSceneName);
        while (!aquariumScene.isLoaded) yield return null;
        yield return new WaitForEndOfFrame();

        // ONLY disable Cameras and Canvases on boot. Let the art meshes stay on since they are spaced far apart!
        foreach (GameObject rootObj in aquariumScene.GetRootGameObjects())
        {
            foreach (Canvas c in rootObj.GetComponentsInChildren<Canvas>(true)) c.enabled = false;
            foreach (Camera cam in rootObj.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
        }

        isSceneLoading = false;
        LocateMyPaired2DManager();
        Toggle2DAquariumVisibility(false);
    }

    private System.Collections.IEnumerator WaitForSceneAndLink()
    {
        Scene aquariumScene = SceneManager.GetSceneByName(aquariumSceneName);
        while (!aquariumScene.isLoaded || isSceneLoading) yield return null;
        LocateMyPaired2DManager();
        Toggle2DAquariumVisibility(false);
    }

    private void LocateMyPaired2DManager()
    {
        AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsSortMode.None);
        foreach (AquariumManager manager in allManagers)
        {
            if (manager != null && manager.tankID == this.tankID)
            {
                myPaired2DManager = manager;
                break;
            }
        }
    }

    void Update()
    {
        if (isPlayerNearby && !isViewingTank && Input.GetKeyDown(KeyCode.E)) EnterAquariumView();
        else if (isViewingTank && Input.GetKeyDown(KeyCode.Q)) ExitAquariumView();
    }

    void EnterAquariumView()
    {
        isViewingTank = true;
        if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        if (localPlayer != null) localPlayer.SetPlayerLockState(true);
        if (Camera.main != null) Camera.main.gameObject.SetActive(false);

        Toggle2DAquariumVisibility(true);

        HUD3DController hud3D = FindFirstObjectByType<HUD3DController>();
        if (hud3D != null) hud3D.SetMoneyTextVisibility(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitAquariumView()
    {
        isViewingTank = false;
        if (localPlayer == null) localPlayer = FindFirstObjectByType<PlayerController3D>();

        if (localPlayer != null)
        {
            localPlayer.SetPlayerLockState(false);
            if (localPlayer.playerCamera != null)
            {
                Camera playerCam = localPlayer.playerCamera.GetComponent<Camera>();
                if (playerCam != null) playerCam.gameObject.SetActive(true);
            }
        }

        Toggle2DAquariumVisibility(false);

        HUD3DController hud3D = FindFirstObjectByType<HUD3DController>();
        if (hud3D != null) hud3D.SetMoneyTextVisibility(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =========================================================
    // --- THE FIX: THE GLOBAL SLEDGEHAMMER ---
    // =========================================================
    void Toggle2DAquariumVisibility(bool makeVisible)
    {
        if (myPaired2DManager == null) LocateMyPaired2DManager();
        if (myPaired2DManager == null) return;

        // 1. If we are turning ON a tank, aggressively turn OFF every other tank in the entire game first.
        if (makeVisible)
        {
            AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsSortMode.None);
            foreach (AquariumManager mgr in allManagers)
            {
                mgr.isTankVisible = false;
                if (mgr.tankCamera != null) mgr.tankCamera.enabled = false;
                if (mgr.mainTankCanvas != null) mgr.mainTankCanvas.enabled = false;
            }
        }

        // 2. Now definitively turn ON (or OFF) this specific tank's Explicit Links
        myPaired2DManager.isTankVisible = makeVisible;

        if (myPaired2DManager.tankCamera != null) 
            myPaired2DManager.tankCamera.enabled = makeVisible;

        if (myPaired2DManager.mainTankCanvas != null) 
            myPaired2DManager.mainTankCanvas.enabled = makeVisible;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = other.GetComponent<Collider>().GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();
        if (player != null)
        {
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
            isPlayerNearby = false;
            if (!isViewingTank) localPlayer = null;
            if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        }
    }
}