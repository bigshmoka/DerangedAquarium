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

    // --- ANIMATION SHIELD TRACKERS ---
    private bool isAnimating = false;
    private float currentSizePercent = 100f;
    private Color currentFrameColor;
    private int snapshotBalance = 0;

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

        // Take a clean snapshot of the wallet balance right as the quest completes
        if (GlobalEconomyManager.Instance != null)
        {
            snapshotBalance = GlobalEconomyManager.Instance.GetBalance();
        }
        else
        {
            snapshotBalance = 100;
        }

        // If an animation loop is already running, safely stop it first to reset timers cleanly
        if (activePopCoroutine != null)
        {
            StopCoroutine(activePopCoroutine);
        }
        
        isAnimating = true;
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

            // Calculate state targets for the LateUpdate text renderer
            currentSizePercent = Mathf.Lerp(100f, maxSizePercent, progress);
            currentFrameColor = numbersFlashColor;

            yield return null;
        }
        currentSizePercent = maxSizePercent;
        currentFrameColor = numbersFlashColor;

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
            currentSizePercent = Mathf.Lerp(maxSizePercent, 100f, progress);

            // Interpolate color values back to native text layout settings
            currentFrameColor = Color.Lerp(numbersFlashColor, originalTextColor, progress);

            yield return null;
        }

        // --- STAGE 4: CLEAN UP RESTORATION ---
        RestoreCleanText();
    }

    // LateUpdate executes AFTER all standard updates, physics, and coin pickups have processed.
    // This allows us to overwrite any accidental text changes before the frame is drawn on screen!
    void LateUpdate()
    {
        if (!isAnimating || textMesh == null) return;

        // Convert the current frame's animated color into a clean hex string
        string hexColorCode = ColorUtility.ToHtmlStringRGB(currentFrameColor);

        // Enforce the visual shield: "Money: $" is written raw outside the tags, locked in place.
        // The sizing and coloring tags wrap ONLY our static quest-reward snapshot balance integer.
        textMesh.text = $"Money: $<size={currentSizePercent:F0}%><color=#{hexColorCode}>{snapshotBalance}</color></size>";
    }

    private void RestoreCleanText()
    {
        isAnimating = false;

        if (textMesh == null) return;

        // Drop the shield and hard-reset the text to your absolute true current wallet balance
        // (This seamlessly catches and displays any coins picked up during the animation window!)
        int trueBalance = GlobalEconomyManager.Instance != null ? GlobalEconomyManager.Instance.GetBalance() : 100;
        textMesh.text = "Money: $" + trueBalance;
        
        activePopCoroutine = null;
    }
}