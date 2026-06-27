using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Quest
{
    public string questID;         // Maps to triggers: "feed_fish", "buy_creatures", "place_decor"
    public string description;     
    public int targetCount;        
    public int currentCount;       
    public int cashReward;         
    public bool isCompleted;
}

[System.Serializable]
public class QuestTemplate
{
    public string questID;
    public string descriptionTemplate; // e.g., "Feed fish {0} times"
    public int baseTargetCount;
    public int baseCashReward;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest Configuration")]
    public int maxActiveQuests = 3;
    [Tooltip("Length of an in-game day cycle tracked in real-world seconds (86400s = 24 hours).")]
    public float dayDurationInSeconds = 86400f; 

    public List<Quest> activeQuests = new List<Quest>();
    private List<QuestTemplate> questPool = new List<QuestTemplate>();

    private float dayTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // --- FIXED: THE PROGRAMMATIC OVERRIDE SHIELD ---
        // This forces 24 hours in pure code, breaking Unity's Inspector serialization lock automatically!
        dayDurationInSeconds = 86400f;

        InitializeQuestPool();
        GenerateNewDailyQuests();
        dayTimer = dayDurationInSeconds;
    }

    void Update()
    {
        // Automatically counts down and natively respects the dev console's timescale cheat!
        dayTimer -= Time.deltaTime;
        if (dayTimer <= 0f)
        {
            RotateDailyQuests();
        }
    }

    void InitializeQuestPool()
    {
        questPool.Clear();
        questPool.Add(new QuestTemplate { questID = "feed_fish", descriptionTemplate = "Feed fish {0} times", baseTargetCount = 10, baseCashReward = 500 });
        questPool.Add(new QuestTemplate { questID = "buy_creatures", descriptionTemplate = "Buy {0} new aquatic creatures", baseTargetCount = 3, baseCashReward = 1500 });
        questPool.Add(new QuestTemplate { questID = "place_decor", descriptionTemplate = "Place {0} item or room decoration", baseTargetCount = 1, baseCashReward = 1000 });
        questPool.Add(new QuestTemplate { questID = "feed_fish", descriptionTemplate = "Feed fish a massive feast ({0} times)", baseTargetCount = 20, baseCashReward = 1200 });
        questPool.Add(new QuestTemplate { questID = "place_decor", descriptionTemplate = "Re-decorate your storefront layout ({0} furniture pieces)", baseTargetCount = 3, baseCashReward = 2500 });
    }

    public void RotateDailyQuests()
    {
        Debug.Log("<color=yellow>[Quest Rotation]</color> A new day has arrived! Clearing and rotating daily quest lists...");
        GenerateNewDailyQuests();
        dayTimer = dayDurationInSeconds;
    }

    void GenerateNewDailyQuests()
    {
        activeQuests.Clear();
        
        // Copy our core templates so we can track and shift out values
        List<QuestTemplate> samplePool = new List<QuestTemplate>(questPool);
        
        for (int i = 0; i < maxActiveQuests; i++)
        {
            if (samplePool.Count == 0) break;
            
            int randomIndex = Random.Range(0, samplePool.Count);
            QuestTemplate template = samplePool[randomIndex];
            
            // Shifting the selected template out of the copy pool prevents duplicate quest types simultaneously
            samplePool.RemoveAt(randomIndex); 

            Quest newQuest = new Quest
            {
                questID = template.questID,
                description = string.Format(template.descriptionTemplate, template.baseTargetCount),
                targetCount = template.baseTargetCount,
                currentCount = 0,
                cashReward = template.baseCashReward,
                isCompleted = false
            };
            
            activeQuests.Add(newQuest);
        }
    }

    public void ProgressQuest(string id, int amount)
    {
        foreach (Quest q in activeQuests)
        {
            if (q.questID == id && !q.isCompleted)
            {
                q.currentCount += amount;
                Debug.Log($"<color=green>[Quest Progress]</color> {q.description}: ({q.currentCount}/{q.targetCount})");

                if (q.currentCount >= q.targetCount)
                {
                    CompleteQuest(q);
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
    }

    // Exposed math method allowing the developer console panel to pull formatting numbers
    public string GetTimeRemainingString()
    {
        if (dayTimer < 0f) return "00h 00m 00s";
        
        int hours = Mathf.FloorToInt(dayTimer / 3600f);
        int minutes = Mathf.FloorToInt((dayTimer % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(dayTimer % 60f);
        
        return string.Format("{0:00}h {1:00}m {2:00}s", hours, minutes, seconds);
    }
}