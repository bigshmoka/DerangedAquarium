using UnityEngine;
using UnityEngine.SceneManagement;

public class TankInteraction3D : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string aquariumSceneName = "AquariumScene";

    [Header("Tank Multi-Instance Tracking")]
    public string tankID = "Unassigned_Tank";

    [Header("UI Prompt")]
    public GameObject pressEPromptUI;

    private bool isPlayerNearby = false;
    private bool isViewingTank = false;
    private PlayerController3D localPlayer;
    
    private AquariumManager myPaired2DManager;
    private static bool isSceneLoading = false;
    private bool isInitialized = false;

    void Start()
    {
        if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        
        if (tankID != "Unassigned_Tank")
        {
            InitializeRuntimeTank(tankID);
        }
    }

    public void InitializeRuntimeTank(string newTankID)
    {
        tankID = newTankID;
        isInitialized = true;

        Scene aquariumScene = SceneManager.GetSceneByName(aquariumSceneName);
        
        if (!aquariumScene.isLoaded && !isSceneLoading)
        {
            isSceneLoading = true;
            Debug.Log($"<color=yellow>[Shop Runtime]</color> Spawning prefab instance. Loading scene: <b>{aquariumSceneName}</b>...");
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
        AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AquariumManager manager in allManagers)
        {
            if (manager != null && manager.tankID == this.tankID)
            {
                myPaired2DManager = manager;
                myPaired2DManager.gameObject.SetActive(true);
                Debug.Log($"<color=green>[Link Success]</color> Placed Prefab Shell successfully bound and activated 2D workspace: <b>{tankID}</b>!");
                break;
            }
        }

        if (myPaired2DManager == null)
        {
            Debug.LogError($"<color=red>[Link Failure]</color> Placed Prefab shell couldn't find a 2D template named: '{tankID}'!");
        }
    }

    void Update()
    {
        if (!isInitialized) return; 

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

        // ===================================================================
        // --- THE UI FLUSH FIX ---
        // Forces the current aquarium to completely reset its selection 
        // tools and close open windows before turning the screen camera off!
        // ===================================================================
        if (myPaired2DManager != null)
        {
            myPaired2DManager.ResetTankUIState();
        }

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
        
        if (pressEPromptUI != null && isPlayerNearby) pressEPromptUI.SetActive(true);
    }

    void Toggle2DAquariumVisibility(bool makeVisible)
    {
        if (myPaired2DManager == null) LocateMyPaired2DManager();
        if (myPaired2DManager == null) return;

        if (makeVisible)
        {
            AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AquariumManager mgr in allManagers)
            {
                if (mgr != myPaired2DManager)
                {
                    mgr.isTankVisible = false;
                    if (mgr.tankCamera != null) mgr.tankCamera.enabled = false;
                    if (mgr.mainTankCanvas != null) mgr.mainTankCanvas.enabled = false;
                    
                    // Defensive sweep: Ensure any background tank UI is completely flushed clean
                    mgr.ResetTankUIState();
                }
            }
        }

        myPaired2DManager.isTankVisible = makeVisible;

        if (myPaired2DManager.tankCamera != null) 
            myPaired2DManager.tankCamera.enabled = makeVisible;

        if (myPaired2DManager.mainTankCanvas != null) 
            myPaired2DManager.mainTankCanvas.enabled = makeVisible;

        foreach (Renderer r in myPaired2DManager.GetComponentsInChildren<Renderer>(true)) 
        {
            r.enabled = makeVisible;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = other.GetComponent<Collider>().GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();

        if (player != null)
        {
            isPlayerNearby = true;
            localPlayer = player;
            Debug.Log($"<color=green>[Trigger Zone]</color> Near placed shell: <b>{tankID}</b>");
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
            Debug.Log($"<color=orange>[Trigger Zone]</color> Left placed shell: <b>{tankID}</b>");
            if (!isViewingTank) localPlayer = null;
            if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        }
    }
}