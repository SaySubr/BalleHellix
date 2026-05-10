using System;
using Data;
using UnityEngine;

public class StoreController : MonoBehaviour
{
    [SerializeField] private SkinConfig skinConfig;
    [SerializeField] private CoinsViewer coinsViewer;

    public Action<SkinTarget, int> OnSelectedSkin;
    public Action<int> OnCoinsChanged;

    private DataController Data => DataController.Instance;

    private void Awake()
    {
        if (coinsViewer == null)
            coinsViewer = FindFirstObjectByType<CoinsViewer>();

        EnsureDefaultSkins();
        RefreshCoins();
    }

    public ESkinState CheckSkinState(SkinTarget target, int id)
    {
        if (Data == null)
            return ESkinState.ForSale;

        if (!Data.IsSkinPurchased(target, id))
            return ESkinState.ForSale;

        return Data.GetSelectedSkin(target) == id ? ESkinState.Selected : ESkinState.Purchased;
    }

    public bool PurchaseOrSelect(SkinTarget target, int id, int cost)
    {
        if (Data == null)
        {
            Debug.LogWarning("StoreController: DataController is missing.");
            return false;
        }

        if (Data.IsSkinPurchased(target, id))
        {
            Data.SelectSkin(target, id);
            OnSelectedSkin?.Invoke(target, id);
            Debug.Log($"StoreController: selected {target} skin {id}.");
            return true;
        }

        if (!Data.SpendCoins(cost))
        {
            Debug.LogWarning($"StoreController: not enough coins for {target} skin {id}. Cost: {cost}, coins: {Data.Coins}.");
            return false;
        }

        Data.PurchaseSkin(target, id);
        Data.SelectSkin(target, id);
        OnSelectedSkin?.Invoke(target, id);
        RefreshCoins();
        Debug.Log($"StoreController: purchased and selected {target} skin {id} for {cost} coins.");
        return true;
    }

    public void RefreshCoins()
    {
        int coins = Data != null ? Data.Coins : 0;
        coinsViewer?.Display(coins);
        OnCoinsChanged?.Invoke(coins);
    }

    private void EnsureDefaultSkins()
    {
        if (Data == null || skinConfig == null || skinConfig.Groups == null)
            return;

        for (int i = 0; i < skinConfig.Groups.Length; i++)
        {
            SkinConfig.SkinGroup group = skinConfig.Groups[i];
            if (group == null || group.skins == null || group.skins.Length == 0 || group.skins[0] == null)
                continue;

            bool selectedExists = skinConfig.GetSkin(group.target, Data.GetSelectedSkin(group.target)) != null;
            Data.EnsureSkinPurchased(group.target, group.skins[0].id, !selectedExists);
        }
    }
}

public enum ESkinState
{
    ForSale,
    Purchased,
    Selected
}
