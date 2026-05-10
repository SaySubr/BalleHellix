using TMPro;
using UnityEngine;

public class SkinViewer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text targetHeader;
    [SerializeField] private TMP_Text header;
    [SerializeField] private TMP_Text cost;

    [Header("Selector")]
    [SerializeField] private SkinSelector skinSelector;

    private void OnEnable()
    {
        if (skinSelector == null)
            return;

        skinSelector.OnChangeHeaderSkin += HandleChangeHeaderSkin;
        if (skinSelector.StoreController != null)
            skinSelector.StoreController.OnSelectedSkin += HandleSelectedSkin;
    }

    private void OnDisable()
    {
        if (skinSelector == null)
            return;

        skinSelector.OnChangeHeaderSkin -= HandleChangeHeaderSkin;
        if (skinSelector.StoreController != null)
            skinSelector.StoreController.OnSelectedSkin -= HandleSelectedSkin;
    }

    private void HandleChangeHeaderSkin(SkinTarget target, int id, string skinName, int skinCost, ESkinState skinState)
    {
        if (targetHeader != null)
            targetHeader.text = target == SkinTarget.HelixBall ? "Helix Ball" : "Fireball Tank";

        if (header != null)
            header.text = skinName;

        if (cost == null)
            return;

        if (skinState == ESkinState.ForSale)
            cost.text = skinCost.ToString();
        else if (skinState == ESkinState.Selected)
            cost.text = "selected";
        else if (skinState == ESkinState.Purchased)
            cost.text = "can select";
    }

    private void HandleSelectedSkin(SkinTarget target, int id)
    {
        if (target == skinSelector.CurrentTarget && cost != null)
            cost.text = "selected";
    }
}
