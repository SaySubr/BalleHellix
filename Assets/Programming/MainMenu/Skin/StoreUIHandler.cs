using UnityEngine;
using MainMenu;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class StoreUIHandler : MonoBehaviour
{
    [SerializeField] private SkinSelector skinSelector;
    [SerializeField] private StoreController controller;

    private void Awake()
    {
        EnsureEventSystem();

        if (skinSelector == null)
            skinSelector = FindFirstObjectByType<SkinSelector>();

        if (controller == null)
            controller = FindFirstObjectByType<StoreController>();
    }

    public void OnClick_NextSkin()
    {
        skinSelector?.ToNext();
    }

    public void OnClick_PrevSkin()
    {
        skinSelector?.ToPrev();
    }

    public void OnClick_NextTarget()
    {
        skinSelector?.ToNextTarget();
    }

    public void OnClick_PrevTarget()
    {
        skinSelector?.ToPrevTarget();
    }

    public void OnClick_ToMainMenu()
    {
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void OnClick_PurchaseOrSelect()
    {
        if (skinSelector == null)
            skinSelector = FindFirstObjectByType<SkinSelector>();

        if (controller == null)
            controller = FindFirstObjectByType<StoreController>();

        if (skinSelector == null || controller == null || skinSelector.CurrentSkin == null)
        {
            Debug.LogWarning("StoreUIHandler: cannot purchase/select because selector, controller, or current skin is missing.");
            return;
        }

        SkinConfig.SkinItem skin = skinSelector.CurrentSkin;
        bool success = controller.PurchaseOrSelect(skinSelector.CurrentTarget, skin.id, skin.cost);
        if (!success)
            Debug.LogWarning($"StoreUIHandler: failed to purchase/select {skinSelector.CurrentTarget} skin {skin.id}. Check coins and SkinConfig.");
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
