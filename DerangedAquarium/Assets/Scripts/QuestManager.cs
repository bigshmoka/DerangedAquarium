using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Quest
{
    [Header("Quest Configuration")]
    [Tooltip("Core key identifier linking to code event triggers ('feed_fish', 'buy_creatures', 'place_decor').")]
    public string questID;         
    [Tooltip("The objective text printed directly to the developer logs and menus.")]
    public string description;     
    [Tooltip("The completion threshold quota target value.")]
    public int targetCount;        
    [Tooltip("The balance ledger cash payout dropped upon passing the milestone target.")]
    public int cashReward;         

    [Header("Live Progress (Read-Only During Play Mode)")]
    [Tooltip("Live runtime count tracking how many tasks have been successfully processed.")]
    public int currentCount; 
    [Tooltip("Live tracking flag displaying whether this individual quest chapter has been passed.")]
    public bool isCompleted; 
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Campaign Storyline Chain")]
    [Tooltip("Design your sequential timeline of story milestones here! Drag, drop, add, or delete elements directly inside the editor.")]
    public List<Quest> masterQuestChain = new List<Quest>();

    [Header("Runtime Status Tracking")]
    [Tooltip("The single quest currently active and presented to the player.")]
    public List<Quest> activeQuests = new List<Quest>();

    private int currentChainIndex = 0;
    private bool isTransitioningQuest = false; // Prevents double-triggering during the 1-second delay window

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // SAFE INSPECTOR INTEGRATION SHIELD
        if (masterQuestChain == null || masterQuestChain.Count == 0)
        {
            InitializeDefaultCampaignChain();
        }

        LoadCurrentQuestFromChain();
    }

    void InitializeDefaultCampaignChain()
    {
        masterQuestChain.Clear();

        // Stage 1: The Basics (Aquarium Introduction)
        masterQuestChain.Add(new Quest { questID = "feed_fish", description = "Get familiar with your tank (Feed fish 3 times)", targetCount = 3, cashReward = 300 });
        masterQuestChain.Add(new Quest { questID = "buy_creatures", description = "Expand your collection (Buy your first new creature)", targetCount = 1, cashReward = 600 });
        
        // Stage 2: Storefront Presentation (3D Interior Integration)
        masterQuestChain.Add(new Quest { questID = "place_decor", description = "Welcome your guests (Place a piece of shop furniture)", targetCount = 1, cashReward = 1000 });
        masterQuestChain.Add(new Quest { questID = "feed_fish", description = "Maintain a healthy routine (Feed fish 15 times)", targetCount = 15, cashReward = 1200 });
        
        // Stage 3: Tycoon Scaling (Advanced Management Milestone)
        masterQuestChain.Add(new Quest { questID = "buy_creatures", description = "Populate your showroom displays (Purchase 3 more creatures)", targetCount = 3, cashReward = 2500 });
        masterQuestChain.Add(new Quest { questID = "place_decor", description = "Expand shop appeal layouts (Place 3 decorations)", targetCount = 3, cashReward = 3000 });
    }

    void LoadCurrentQuestFromChain()
    {
        activeQuests.Clear();

        if (currentChainIndex < masterQuestChain.Count)
        {
            // Reset state trackers on load to wipe residual inspector initialization remnants
            masterQuestChain[currentChainIndex].currentCount = 0;
            masterQuestChain[currentChainIndex].isCompleted = false;

            // Pull the exact current milestone out of our sequential timeline index row
            activeQuests.Add(masterQuestChain[currentChainIndex]);
            Debug.Log($"<color=cyan>[Quest Chain]</color> Active Story Objective Updated: {masterQuestChain[currentChainIndex].description}");
        }
        else
        {
            // Campaign Epilogue Fallback State if a player beats all your built levels
            Quest sandboxQuest = new Quest 
            { 
                questID = "completed", 
                description = "All story chapters completed! Enjoy your Sandbox Aquarium.", 
                targetCount = 9999, 
                cashReward = 0
            };
            sandboxQuest.isCompleted = true;
            activeQuests.Add(sandboxQuest);
        }
    }

    public void ProgressQuest(string id, int amount)
    {
        // Block new progress from coming in while we are waiting out our 1-second "COMPLETE" screen delay
        if (isTransitioningQuest) return;

        foreach (Quest q in activeQuests)
        {
            if (q.questID == id && !q.isCompleted)
            {
                q.currentCount += amount;
                Debug.Log($"<color=green>[Quest Progress]</color> {q.description}: ({q.currentCount}/{q.targetCount})");

                if (q.currentCount >= q.targetCount)
                {
                    CompleteQuest(q);
                    // Break out of the loop instantly so we don't evaluate a collection that was just modified by LoadCurrentQuestFromChain!
                    break; 
                }
            }
        }
    }

    void CompleteQuest(Quest q)
    {
        q.isCompleted = true;
        Debug.Log($"<color=gold><b>*** QUEST COMPLETED: {q.description}! ***</b></color>");

        if (GlobalEconomyManager.Instance != null)
        {
            GlobalEconomyManager.Instance.AddMoney(q.cashReward);
            Debug.Log($"[Quest Reward] +${q.cashReward} deposited.");
        }

        // --- NEW: START THE 1-SECOND DELAY TIMING ENGINE ---
        // Instead of instantly loading the next quest, start our coroutine timer clock!
        StartCoroutine(DelayedNextQuestTransition());
    }

    private IEnumerator DelayedNextQuestTransition()
    {
        isTransitioningQuest = true;
        
        // Wait on the game clock for exactly 1.0 seconds
        yield return new WaitForSeconds(1.0f);
        
        currentChainIndex++;
        LoadCurrentQuestFromChain();
        
        isTransitioningQuest = false;
    }

    // This allows your developer console's 'skipquest' command to act as an instant skip tool!
    public void RotateDailyQuests()
    {
        if (currentChainIndex < masterQuestChain.Count)
        {
            Debug.Log($"<color=orange>[Dev Tooling]</color> Force-skipping campaign quest chapter: '{masterQuestChain[currentChainIndex].description}'");
            currentChainIndex++;
            LoadCurrentQuestFromChain();
        }
    }

    // Repurposed string handler prevents your console screen UI code from throwing script reference errors
    public string GetTimeRemainingString()
    {
        if (currentChainIndex >= masterQuestChain.Count) return "CAMPAIGN COMPLETED";
        return $"Chapter {currentChainIndex + 1} / {masterQuestChain.Count}";
    }
}