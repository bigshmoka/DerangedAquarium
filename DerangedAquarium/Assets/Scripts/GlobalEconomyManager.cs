using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GlobalEconomyManager : MonoBehaviour
{
    public static GlobalEconomyManager Instance { get; private set; }

    [Header("Default Wallet Configuration")]
    public int defaultStartingMoney = 100;

    private int currentWalletBalance;
    private bool isInitialized = false;

    private List<TMP_Text> registeredUITextComponents = new List<TMP_Text>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // --- THE FIXED LINE ---
        // This forces the object to instantly clear any parent assignments at runtime.
        // It guarantees the object is a 'Root GameObject' before moving it safely to DontDestroyOnLoad!
        transform.SetParent(null); 

        DontDestroyOnLoad(gameObject); 

        if (!isInitialized)
        {
            currentWalletBalance = defaultStartingMoney;
            isInitialized = true;
        }
    }

    public int GetBalance() => currentWalletBalance;

    public void AddMoney(int amount)
    {
        currentWalletBalance += amount;
        UpdateAllRegisteredDisplays();
    }

    public bool TrySpendMoney(int amount)
    {
        if (currentWalletBalance >= amount)
        {
            currentWalletBalance -= amount;
            UpdateAllRegisteredDisplays();
            return true;
        }
        return false;
    }

    public void DeductMoney(int amount)
    {
        currentWalletBalance -= amount;
        if (currentWalletBalance < 0) currentWalletBalance = 0;
        UpdateAllRegisteredDisplays();
    }

    public void RegisterWalletDisplay(TMP_Text textElement)
    {
        if (textElement != null && !registeredUITextComponents.Contains(textElement))
        {
            registeredUITextComponents.Add(textElement);
            textElement.text = "Money: $" + currentWalletBalance; 
        }
    }

    public void UnregisterWalletDisplay(TMP_Text textElement)
    {
        if (registeredUITextComponents.Contains(textElement))
        {
            registeredUITextComponents.Remove(textElement);
        }
    }

    private void UpdateAllRegisteredDisplays()
    {
        registeredUITextComponents.RemoveAll(textMesh => textMesh == null);

        foreach (TMP_Text textComponent in registeredUITextComponents)
        {
            textComponent.text = "Money: $" + currentWalletBalance;
        }
    }
}