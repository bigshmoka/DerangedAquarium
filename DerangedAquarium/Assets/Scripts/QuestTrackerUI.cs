using UnityEngine;
using TMPro;

public class QuestTrackerUI : MonoBehaviour
{
    [Header("UI Text Mesh Component")]
    [Tooltip("Drag the TextMeshPro component that will print the objective text here.")]
    public TMP_Text questDisplayText;

    void Start()
    {
        // Fallback auto-assignment: if forgotten in the inspector, grab the component directly
        if (questDisplayText == null)
        {
            questDisplayText = GetComponent<TMP_Text>();
        }
    }

    void Update()
    {
        // Safety guard: Don't execute if the text field or the manager aren't active yet
        if (QuestManager.Instance == null || questDisplayText == null)
        {
            return;
        }

        // Pull the live active quest list from the master system authority blueprint
        if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
        {
            Quest currentQuest = QuestManager.Instance.activeQuests[0];

            // Condition 1: If the player has finished the entire sequence chain game database array
            if (currentQuest.questID == "completed")
            {
                questDisplayText.text = "<b><color=#55FF55>🥇 CAMPAIGN BEATEN!</color></b>\n" +
                                        "<size=85%>All chapters cleared. Enjoy your Sandbox Aquarium!</size>";
            }
            // --- NEW: Condition 2: If the quest was just completed, lock the display to green COMPLETE text ---
            else if (currentQuest.isCompleted)
            {
                // FIXED: Removed the "[ ]" decoration header entirely
                questDisplayText.text = $"<b><color=#33CCFF>CURRENT OBJECTIVE</color></b>\n" +
                                        $"{currentQuest.description}\n" +
                                        $"<b><color=#55FF55>COMPLETE</color></b>";
            }
            // Condition 3: Regular active quest progress display state
            else
            {
                // FIXED: Removed the "[ ]" decoration header entirely
                questDisplayText.text = $"<b><color=#33CCFF>CURRENT OBJECTIVE</color></b>\n" +
                                        $"{currentQuest.description}\n" +
                                        $"<b><color=#FFCC00>Progress: {currentQuest.currentCount} / {currentQuest.targetCount}</color></b>\n" +
                                        $"Reward: <color=#66FF66>${currentQuest.cashReward}</color>";
            }
        }
        else
        {
            questDisplayText.text = "Searching for active quest updates...";
        }
    }
}