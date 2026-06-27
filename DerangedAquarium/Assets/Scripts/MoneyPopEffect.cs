using UnityEngine;
using System.Collections;
using TMPro;

public class MoneyPopEffect : MonoBehaviour
{
    [Header("Juice Animation Settings")]
    [Tooltip("How much larger the numeric characters grow (e.g., 1.4 means 140% font size).")]
    public float scaleMultiplier = 1.4f;
    [Tooltip("Time in seconds for the digits to expand outward.")]
    public float growDuration = 0.08f;
    [Tooltip("How long the numbers stay fully expanded and fully green before starting to shrink.")]
    public float lingerDuration = 0.5f; 
    [Tooltip("Time in seconds for the digits to shrink back to their original size.")]
    public float shrinkDuration = 0.28f;

    [Header("Color Flash Settings")]
    [Tooltip("The color the numeric digits will shift to when money is added.")]
    public Color numbersFlashColor = new Color(0.333f, 1f, 0.333f, 1f); // Vibrant Green (#55FF55)

    private TMP_Text textMesh;
    private Color originalTextColor;
    private Coroutine activePopCoroutine;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (textMesh != null)
        {
            originalTextColor = textMesh.color;
        }
    }

    void OnEnable()
    {
        // Subscribe to the global quest completion event channel
        QuestManager.OnQuestRewardPaid += TriggerPopEffect;
    }

    void OnDisable()
    {
        // Always unregister when disabled or switching scenes to prevent memory leaks
        QuestManager.OnQuestRewardPaid -= TriggerPopEffect;
    }

    private void TriggerPopEffect()
    {
        if (textMesh == null) return;

        // If an animation loop is already running, safely stop it first to prevent overlapping glitches
        if (activePopCoroutine != null)
        {
            StopCoroutine(activePopCoroutine);
        }
        
        activePopCoroutine = StartCoroutine(AnimatePopAndColorSequence());
    }

    private IEnumerator AnimatePopAndColorSequence()
    {
        float elapsed = 0f;
        float maxSizePercent = scaleMultiplier * 100f; // Converts 1.4f to 140%

        // --- STAGE 1: QUICK TEXT EXPANSION PUNCH ---
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / growDuration;

            // Calculate the growing font percentage size smoothly
            float currentSizePercent = Mathf.Lerp(100f, maxSizePercent, progress);

            // Reconstruct the text safely using live values
            UpdateMoneyTextWithEffects(currentSizePercent, numbersFlashColor);

            yield return null;
        }
        UpdateMoneyTextWithEffects(maxSizePercent, numbersFlashColor);

        // --- STAGE 2: THE HOLD / LINGER WINDOW ---
        // Keeps the numbers hanging at max size and maximum green brightness for half a second
        yield return new WaitForSeconds(lingerDuration);

        // --- STAGE 3: SMOOTH SETTLE & SHRINK BACK TO NORMAL ---
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shrinkDuration;

            // Interpolate sizing tags down from expanded threshold back to base 100%
            float currentSizePercent = Mathf.Lerp(maxSizePercent, 100f, progress);

            // Interpolate color values back to native text layout settings
            Color currentFrameColor = Color.Lerp(numbersFlashColor, originalTextColor, progress);
            
            UpdateMoneyTextWithEffects(currentSizePercent, currentFrameColor);

            yield return null;
        }

        // --- STAGE 4: CLEAN UP RESTORATION ---
        RestoreCleanText();
    }

    private void UpdateMoneyTextWithEffects(float sizePercent, Color targetColor)
    {
        if (textMesh == null) return;

        // Fetch the up-to-the-millisecond accurate balance directly from your global wallet singleton
        int currentBalance = 100;
        if (GlobalEconomyManager.Instance != null)
        {
            currentBalance = GlobalEconomyManager.Instance.GetBalance();
        }

        // Convert the color structure into a clean hexadecimal string
        string hexColorCode = ColorUtility.ToHtmlStringRGB(targetColor);

        // BULLETPROOF RECONSTRUCTION: "Money: $" is written completely raw outside of the tags!
        // The sizing and coloring tags wrap ONLY the numeric balance integers.
        textMesh.text = $"Money: $<size={sizePercent:F0}%><color=#{hexColorCode}>{currentBalance}</color></size>";
    }

    private void RestoreCleanText()
    {
        if (textMesh == null) return;

        // Hard reset to a pristine, un-tagged string layout matching your default setup
        int currentBalance = GlobalEconomyManager.Instance != null ? GlobalEconomyManager.Instance.GetBalance() : 100;
        textMesh.text = "Money: $" + currentBalance;
        
        activePopCoroutine = null;
    }
}