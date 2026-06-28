using UnityEngine;
using TMPro;

public class QuestTrackerUI : MonoBehaviour
{
    [Header("UI Text Mesh Component")]
    [Tooltip("Drag the TextMeshPro component that will print the objective text here.")]
    public TMP_Text questDisplayText;

    // --- OPTIMIZATION FIELD CACHES ---
    // Stores historical data footprints to completely eliminate redundant per-frame string allocations
    private int lastRecordedCount = -1;
    private bool lastRecordedCompletionState = false;
    private string lastRecordedDescription = "";

    void Start()
    {
        if (questDisplayText == null)
        {
            questDisplayText = GetComponent<TMP_Text>();
        }
    }

    void Update()
    {
        if (QuestManager.Instance == null || questDisplayText == null)
        {
            return;
        }

        if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
        {
            Quest currentQuest = QuestManager.Instance.activeQuests[0];

            // OPTIMIZED GATE: Check if any tracking metrics changed before executing string generations
            if (currentQuest.currentCount == lastRecordedCount && 
                currentQuest.isCompleted == lastRecordedCompletionState && 
                currentQuest.description == lastRecordedDescription &&
                questDisplayText.text != "")
            {
                return; // Memory footprint bypass: Zero garbage generated!
            }

            // Update baseline caches
            lastRecordedCount = currentQuest.currentCount;
            lastRecordedCompletionState = currentQuest.isCompleted;
            lastRecordedDescription = currentQuest.description;

            // Condition 1: If the player has finished the entire sequence chain game database array
            if (currentQuest.questID == "completed")
            {
                questDisplayText.text = "<b><color=#55FF55>CAMPAIGN BEATEN!</color></b>\n" +
                                        "<size=85%>All chapters cleared. Enjoy your Sandbox Aquarium!</size>";
            }
            // Condition 2: If the quest was just completed, lock the display to green COMPLETE text
            else if (currentQuest.isCompleted)
            {
                questDisplayText.text = $"<b><color=#33CCFF>CURRENT OBJECTIVE</color></b>\n" +
                                        $"{currentQuest.description}\n" +
                                        $"<b><color=#55FF55>COMPLETE</color></b>";
            }
            // Condition 3: Regular active quest progress display state
            else
            {
                questDisplayText.text = $"<b><color=#33CCFF>CURRENT OBJECTIVE</color></b>\n" +
                                        $"{currentQuest.description}\n" +
                                        $"<b><color=#FFCC00>Progress: {currentQuest.currentCount} / {currentQuest.targetCount}</color></b>\n" +
                                        $"Reward: <color=#66FF66>${currentQuest.cashReward}</color>";
            }
        }
        else
        {
            if (questDisplayText.text != "Searching for active quest updates...")
            {
                questDisplayText.text = "Searching for active quest updates...";
            }
        }
    }
}