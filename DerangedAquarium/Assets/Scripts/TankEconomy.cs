using UnityEngine;

public class TankEconomy : MonoBehaviour
{
    [HideInInspector] public int totalMoney = 100;
    private TankShopUI shopUI;

    // Explicitly wires the UI reference to bypass Unity AddComponent race conditions
    public void Initialize(TankShopUI targetShopUI)
    {
        shopUI = targetShopUI;
    }

    public void AddMoney(int amount)
    {
        totalMoney += amount;
        UpdateBalanceUI();
    }

    public bool TrySpendMoney(int amount)
    {
        if (totalMoney >= amount)
        {
            totalMoney -= amount;
            UpdateBalanceUI();
            return true;
        }
        return false;
    }

    public void DeductCash(int amount)
    {
        totalMoney -= amount;
        if (totalMoney < 0) totalMoney = 0;
        UpdateBalanceUI();
    }

    public void UpdateBalanceUI()
    {
        if (shopUI != null) shopUI.UpdateMoneyText(totalMoney);
    }
}