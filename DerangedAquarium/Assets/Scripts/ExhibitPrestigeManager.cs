using UnityEngine;
using System.Collections.Generic;

public class ExhibitPrestigeManager : MonoBehaviour
{
    public static ExhibitPrestigeManager Instance { get; private set; }

    [Header("Museum Progression Config")]
    public int currentLevel = 1;
    public int currentPrestigePoints = 0;
    [Tooltip("How many additional prestige points are required per level up (e.g., Level 1 needs 100, Level 2 needs 200).")]
    public int pointsPerLevelMultiplier = 100;

    [Header("Ticket Booth Config")]
    [Tooltip("The amount of cash collected automatically from every single visitor who walks through the front door.")]
    public int currentEntranceFee = 15;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds prestige points to the curator profile and processes level up milestone checks.
    /// </summary>
    public void AddPrestigePoints(int amount)
    {
        currentPrestigePoints += amount;
        Debug.Log($"<color=gold>[Museum Prestige]</color> Earned +{amount} Experience Points! Total: {currentPrestigePoints}");

        // Evaluate level up state machine bounds
        int pointsRequiredForNextLevel = currentLevel * pointsPerLevelMultiplier;
        while (currentPrestigePoints >= pointsRequiredForNextLevel)
        {
            currentPrestigePoints -= pointsRequiredForNextLevel;
            currentLevel++;
            
            // Automatically bump entry ticket prices as your public sanctuary climbs levels!
            currentEntranceFee += 10; 

            Debug.Log($"<color=green><b>*** SANCTUARY LEVEL UP! You are now Level {currentLevel}! ***</b></color>");
            Debug.Log($"[Ticket Booth] Museum prestige increased. Standard Entry Ticket price raised to: ${currentEntranceFee}");
            
            // Recalculate target ceiling loops in case huge experience payloads arrive at once
            pointsRequiredForNextLevel = currentLevel * pointsPerLevelMultiplier;
        }

        // Commands your 3D user canvas layout to immediately update its on-screen text readouts
        HUD3DController localHUD = FindFirstObjectByType<HUD3DController>();
        if (localHUD != null)
        {
            localHUD.UpdatePrestigeVisuals();
        }
    }

    /// <summary>
    /// DIRECT XP CALCULATOR: Removes the 10% multiplication filter entirely. Calculates a flat, 
    /// low-rate XP award that changes instantly by 1 point for every single fish or item present in the tank.
    /// </summary>
    public int CalculateTankRatingScore(AquariumManager tankManager)
    {
        if (tankManager == null) return 0;

        // Baseline minimum XP for looking at an empty clean tank
        int directXPAward = 1;

        // ===================================================================
        // 1. TALLY FISH QUANTITY & SIZE
        // ===================================================================
        NaturalFishAI[] activeFishInTank = tankManager.GetComponentsInChildren<NaturalFishAI>(true);
        foreach (NaturalFishAI fish in activeFishInTank)
        {
            // Every single fish inside the tank dynamically adds +1 XP
            directXPAward += 1;

            // Growth Reward: If you raise a fish past its baby phase (scale >= 1.0f), it gives an extra +1 XP
            if (fish.currentScaleModifier >= 1.0f)
            {
                directXPAward += 1;
            }
        }

        // ===================================================================
        // 2. TALLY DECORATIONS & PLACED ITEMS (Snail, Feeder, Chest, Plants, etc.)
        // ===================================================================
        // Generically scan the tank's local hierarchy for any object utilizing a SpriteRenderer
        SpriteRenderer[] allSpritesInTank = tankManager.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in allSpritesInTank)
        {
            // Skip system UI canvas layers, active fish bodies, or background algae node points
            if (sr.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
            if (sr.GetComponent<NaturalFishAI>() != null) continue;
            if (sr.GetComponent<AlgaeNode>() != null) continue;
            
            // Skip temporary runtime items (falling food pellets, rising air bubbles, placement previews)
            if (sr.name.Contains("Pellet") || sr.name.Contains("Bubble") || sr.name.Contains("Preview")) continue;

            // Every single separate item placed inside this tank directly adds +1 XP!
            // This guarantees your automated feeders, snails, and chests instantly move the XP bar.
            directXPAward += 1;
        }

        // ===================================================================
        // 3. EVALUATE GLASS CLEANLINESS
        // ===================================================================
        AlgaeManager algaeComp = tankManager.algaeManager;
        if (algaeComp != null && algaeComp.algaeNodes != null && algaeComp.algaeNodes.Length > 0)
        {
            float totalAlgaeAccumulation = 0f;
            int validNodeCount = 0;

            foreach (AlgaeNode node in algaeComp.algaeNodes)
            {
                if (node != null)
                {
                    totalAlgaeAccumulation += node.currentAlgaeLevel; 
                    validNodeCount++;
                }
            }

            if (validNodeCount > 0)
            {
                float averageAlgaeDensity = totalAlgaeAccumulation / validNodeCount;

                // If the glass is heavily neglected and covered in green algae, slash the earned XP in half
                if (averageAlgaeDensity >= 0.50f)
                {
                    directXPAward = Mathf.Max(1, directXPAward / 2); 
                }
            }
        }

        return directXPAward; 
    }
}