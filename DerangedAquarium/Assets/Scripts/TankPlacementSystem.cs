using UnityEngine;

public class TankPlacementSystem : MonoBehaviour
{
    [HideInInspector] public bool isPlacingDecoration = false;
    [HideInInspector] public bool isPlacingItem = false;

    private GameObject activeDecorationPreview;
    private GameObject selectedDecorationPrefab;
    private int selectedDecorationCost;

    private GameObject activeItemPreview;
    private GameObject selectedItemPrefab;
    private int selectedItemCost;

    private TankEconomy economy;
    private TankShopUI shopUI;
    
    // Cached camera layer isolation link
    private Camera localTankCamera;
    
    // Multi-tank visibility verification reference link
    private AquariumManager manager;

    public void Initialize(TankEconomy targetEconomy, TankShopUI targetShopUI)
    {
        economy = targetEconomy;
        shopUI = targetShopUI;
        
        // Caches local camera directly parented inside this explicit manager layout setup
        localTankCamera = GetComponentInChildren<Camera>(true);
        
        // Cache the master manager component attached to this GameObject node tree
        manager = GetComponent<AquariumManager>();
    }

    void Update()
    {
        // ===================================================================
        // --- MULTI-TANK PLACEMENT PROTECTION SHIELD ---
        // Prevents inactive background managers from spawning items or capturing
        // clicks intended for the active showroom viewport layout.
        // ===================================================================
        if (manager != null && !manager.isTankVisible) return;

        if (isPlacingDecoration) HandleDecorationPlacement();
        else if (isPlacingItem) HandleItemPlacement();
    }

    private Camera GetActiveCamera()
    {
        // Replaces Camera.main since the 3D player camera component turns off inside aquarium view
        return (localTankCamera != null && localTankCamera.enabled) ? localTankCamera : Camera.main;
    }

    public void StartDecorationPlacement(GameObject prefab, int cost)
    {
        selectedDecorationPrefab = prefab;
        selectedDecorationCost = cost;
        shopUI.CloseShopMenu();

        shopUI.isFeedToolActive = false;
        shopUI.isSpongeToolActive = false;
        shopUI.UpdateFeedButtonUI();
        shopUI.UpdateSpongeButtonUI();

        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        Vector3 mousePos = activeCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        activeDecorationPreview = Instantiate(selectedDecorationPrefab, mousePos, Quaternion.identity, this.transform);
        SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);

        isPlacingDecoration = true;
    }

    private void HandleDecorationPlacement()
    {
        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        Vector3 mousePos = activeCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (activeDecorationPreview != null) activeDecorationPreview.transform.position = mousePos;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(activeDecorationPreview);
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (economy.TrySpendMoney(selectedDecorationCost))
            {
                SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1.0f);

                // --- INTEGRATED: PROGRESS DECOR QUEST FOR 2D DECORATIONS ---
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ProgressQuest("place_decor", 1);
                }

                activeDecorationPreview = null;
                isPlacingDecoration = false;
                selectedDecorationPrefab = null;
            }
        }
    }

    public void StartItemPlacement(GameObject prefab, int cost)
    {
        selectedItemPrefab = prefab;
        selectedItemCost = cost;
        shopUI.CloseShopMenu();

        shopUI.isFeedToolActive = false;
        shopUI.isSpongeToolActive = false;
        shopUI.UpdateFeedButtonUI();
        shopUI.UpdateSpongeButtonUI();

        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        Vector3 mousePos = activeCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        activeItemPreview = Instantiate(selectedItemPrefab, mousePos, Quaternion.identity, this.transform);
        isPlacingItem = true;
    }

    private void HandleItemPlacement()
    {
        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        Vector3 mousePos = activeCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (activeItemPreview != null) activeItemPreview.transform.position = mousePos;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(activeItemPreview);
            isPlacingItem = false;
            selectedItemPrefab = null;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (economy.TrySpendMoney(selectedItemCost))
            {
                // --- INTEGRATED: PROGRESS DECOR QUEST FOR 2D ITEMS (LIKE AUTO FEEDERS) ---
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ProgressQuest("place_decor", 1);
                }

                activeItemPreview = null;
                isPlacingItem = false;
                selectedItemPrefab = null;
            }
        }
    }
}