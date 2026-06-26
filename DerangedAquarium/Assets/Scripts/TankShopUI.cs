using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankShopUI : MonoBehaviour
{
    [HideInInspector] public GameObject shopMenuWindow; 
    [HideInInspector] public TMP_Text moneyText; 
    [HideInInspector] public TMP_Text errorNotificationText; 
    
    [HideInInspector] public Button feedToolButton;       
    [HideInInspector] public TMP_Text feedToolText;       
    [HideInInspector] public Button spongeToolButton;     
    [HideInInspector] public TMP_Text spongeToolText;     

    [HideInInspector] public bool isShopOpen = false; 
    [HideInInspector] public bool isFeedToolActive = false;
    [HideInInspector] public bool isSpongeToolActive = false;

    public void OpenShopMenu() { isShopOpen = true; }
    public void CloseShopMenu() { isShopOpen = false; }

    public void ToggleFeedingTool()
    {
        isFeedToolActive = !isFeedToolActive;
        if (isFeedToolActive) isSpongeToolActive = false;
        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    public void ToggleSpongeTool()
    {
        isSpongeToolActive = !isSpongeToolActive;
        if (isSpongeToolActive) isFeedToolActive = false;
        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    public void UpdateMoneyText(int balance)
    {
        if (moneyText != null) moneyText.text = "Money: $" + balance;
    }

    public void TriggerNotificationAlert(string message)
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.text = message;
            errorNotificationText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNotificationAlert));
            Invoke(nameof(HideNotificationAlert), 2.5f);
        }
    }

    private void HideNotificationAlert()
    {
        if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);
    }

    public void UpdateFeedButtonUI()
    {
        if (feedToolText != null && feedToolButton != null)
        {
            feedToolText.text = isFeedToolActive ? "Feed: ON" : "Feed: OFF";
            feedToolButton.GetComponent<Image>().color = isFeedToolActive ? new Color(0.2f, 0.8f, 0.2f, 1.0f) : new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }

    public void UpdateSpongeButtonUI()
    {
        if (spongeToolText != null && spongeToolButton != null)
        {
            spongeToolText.text = isSpongeToolActive ? "Sponge: ON" : "Sponge: OFF";
            spongeToolButton.GetComponent<Image>().color = isSpongeToolActive ? new Color(0.2f, 0.6f, 0.9f, 1.0f) : new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }
}