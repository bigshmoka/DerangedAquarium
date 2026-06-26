using UnityEngine;

public class TankEconomy : MonoBehaviour
{
    private TankShopUI shopUI;
    private bool hasInitializedDisplay = false;

    public void Initialize(TankShopUI targetShopUI)
    {
        shopUI = targetShopUI;
        TryRegisterDisplayWithGlobalWallet();
    }

    void Start()
    {
        // --- BULLETPROOF FALLBACK ---
        // If Awake initialization order skipped it, this catches and registers
        // the text layout inside Start when references are guaranteed to be set!
        TryRegisterDisplayWithGlobalWallet();
    }

    private void TryRegisterDisplayWithGlobalWallet()
    {
        if (shopUI != null && shopUI.moneyText != null && GlobalEconomyManager.Instance != null && !hasInitializedDisplay)
        {
            GlobalEconomyManager.Instance.RegisterWalletDisplay(shopUI.moneyText);
            hasInitializedDisplay = true;
        }
    }

    public int totalMoney
    {
        get
        {
            return GlobalEconomyManager.Instance != null ? GlobalEconomyManager.Instance.GetBalance() : 100;
        }
        set
        {
            if (GlobalEconomyManager.Instance != null)
            {
                int netDifference = value - GlobalEconomyManager.Instance.GetBalance();
                if (netDifference > 0) GlobalEconomyManager.Instance.AddMoney(netDifference);
                else if (netDifference < 0) GlobalEconomyManager.Instance.DeductMoney(Mathf.Abs(netDifference));
            }
        }
    }

    public void AddMoney(int amount)
    {
        if (GlobalEconomyManager.Instance != null)
            GlobalEconomyManager.Instance.AddMoney(amount);
    }

    public bool TrySpendMoney(int amount)
    {
        if (GlobalEconomyManager.Instance != null)
            return GlobalEconomyManager.Instance.TrySpendMoney(amount);
        return false;
    }

    public void DeductCash(int amount)
    {
        if (GlobalEconomyManager.Instance != null)
            GlobalEconomyManager.Instance.DeductMoney(amount);
    }

    public void UpdateBalanceUI()
    {
        if (shopUI != null && GlobalEconomyManager.Instance != null)
        {
            shopUI.UpdateMoneyText(GlobalEconomyManager.Instance.GetBalance());
        }
    }
}