using UnityEngine;

public class TrashObject : MonoBehaviour
{
    [Header("Trash Profile")]
    public string trashName = "Crumpled Paper";
    
    [Tooltip("Optional: Drop a custom dust or sweeping particle effect prefab here!")]
    public GameObject cleanupParticlePrefab;

    public void SweepUp()
    {
        // 1. Play visual dust/sweep feedback if assigned
        if (cleanupParticlePrefab != null)
        {
            Instantiate(cleanupParticlePrefab, transform.position, Quaternion.identity);
        }

        // 2. Progress your central tycoon quest system automatically!
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressQuest("clean_trash", 1);
        }

        Debug.Log($"[Cleanup] Swept away dirty clutter target: {trashName}");
        
        // 3. Vaporize the trash model from the storefront scene completely
        Destroy(gameObject);
    }
}