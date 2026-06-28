using UnityEngine;
using UnityEngine.SceneManagement;

public class TankInteraction3D : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The EXACT string name of your existing 2D Aquarium scene file.")]
    public string aquariumSceneName = "AquariumScene";

    [Header("Tank Multi-Instance Tracking")]
    [Tooltip("Give this specific physical 3D prop a unique ID that matches its 2D simulation counterpart.")]
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
            Debug.Log($"<color=yellow>[Scene Loader]</color> Initialization sweep: Loading additive scene <b>{aquariumSceneName}</b>...");
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
        AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsSortMode.None);
        foreach (AquariumManager manager in allManagers)
        {
            if (manager != null && manager.tankID == this.tankID)
            {
                myPaired2DManager = manager;
                Debug.Log($"<color=green>[Link Success]</color> Physical 3D Prop <b>{gameObject.name}</b> successfully linked to 2D Manager for <b>{tankID}</b>!");
                break;
            }
        }

        if (myPaired2DManager == null)
        {
            Debug.LogError($"<color=red>[Link Failure]</color> Physical 3D Prop <b>{gameObject.name}</b> could not find a matching 2D manager with tankID: '{tankID}'!");
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
        Debug.Log($"<color=cyan>[View State]</color> Opening view for: <b>{tankID}</b>. Shifting camera matrices and freeing system cursor.");

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
        Debug.Log($"<color=cyan>[View State]</color> Closing view for: <b>{tankID}</b>. Restoring 3D Player viewport and locking mouse controller.");

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
        
        if (pressEPromptUI != null && isPlayerNearby) pressEPromptUI.SetActive(true);
    }

    void Toggle2DAquariumVisibility(bool makeVisible)
    {
        if (myPaired2DManager == null) LocateMyPaired2DManager();
        if (myPaired2DManager == null) return;

        // Aggressive background shielding loop
        if (makeVisible)
        {
            AquariumManager[] allManagers = FindObjectsByType<AquariumManager>(FindObjectsSortMode.None);
            foreach (AquariumManager mgr in allManagers)
            {
                if (mgr != myPaired2DManager)
                {
                    mgr.isTankVisible = false;
                    if (mgr.tankCamera != null) mgr.tankCamera.enabled = false;
                    if (mgr.mainTankCanvas != null) mgr.mainTankCanvas.enabled = false;
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

        Debug.Log($"<color=magenta>[Visibility System]</color> Forced explicit assets for <b>{tankID}</b> to: <b>{makeVisible}</b>");
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = other.GetComponent<Collider>().GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();

        if (player != null)
        {
            isPlayerNearby = true;
            localPlayer = player;
            
            // RESTORED: Entering Range Diagnostic Log
            Debug.Log($"<color=green>[Trigger Zone]</color> Player entered proximity radius of 3D Prop for tank: <b>{tankID}</b>. Prompting UI overlay display.");
            
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
            
            // RESTORED: Leaving Range Diagnostic Log
            Debug.Log($"<color=orange>[Trigger Zone]</color> Player exited proximity radius of 3D Prop for tank: <b>{tankID}</b>. Suppressing UI overlay display.");
            
            if (!isViewingTank) localPlayer = null;
            if (pressEPromptUI != null) pressEPromptUI.SetActive(false);
        }
    }
}