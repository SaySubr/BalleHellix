using System;
using System.Collections;
using Data;
using UnityEngine;

public class SkinSelector : MonoBehaviour
{
    [Header("Camera Move")]
    [SerializeField] private GameObject previewCamera;
    [SerializeField] private float cameraZ = -10f;

    [Header("Controllers")]
    [SerializeField] private SkinDecorator decorator;
    [SerializeField] private StoreController storeController;

    private int _selectedIndex;
    private int _selectedGroupIndex;

    public Action<SkinTarget, int, string, int, ESkinState> OnChangeHeaderSkin;
    public Action<SkinTarget> OnChangeTarget;

    public int SelectedIndex => _selectedIndex;
    public SkinTarget CurrentTarget => decorator != null ? decorator.GetTargetByGroupIndex(_selectedGroupIndex) : SkinTarget.HelixBall;
    public SkinConfig.SkinItem CurrentSkin => GetCurrentSkin();
    public StoreController StoreController => storeController;

    private void Awake()
    {
        if (previewCamera == null && Camera.main != null)
            previewCamera = Camera.main.gameObject;

        if (decorator == null)
            decorator = FindFirstObjectByType<SkinDecorator>();

        if (storeController == null)
            storeController = FindFirstObjectByType<StoreController>();
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        yield return null;

        if (decorator == null)
            yield break;

        SelectSavedSkinIndex();
        CameraMove();
        NotifySelectionChanged();
    }

    private void OnEnable()
    {
        if (storeController != null)
            storeController.OnSelectedSkin += HandleSelectedSkin;
    }

    private void OnDisable()
    {
        if (storeController != null)
            storeController.OnSelectedSkin -= HandleSelectedSkin;
    }

    public void ToNext()
    {
        SkinConfig.SkinItem[] skins = GetCurrentSkins();
        if (skins.Length == 0)
            return;

        _selectedIndex = _selectedIndex < skins.Length - 1 ? _selectedIndex + 1 : 0;
        CameraMove();
        NotifySelectionChanged();
    }

    public void ToPrev()
    {
        SkinConfig.SkinItem[] skins = GetCurrentSkins();
        if (skins.Length == 0)
            return;

        _selectedIndex = _selectedIndex > 0 ? _selectedIndex - 1 : skins.Length - 1;
        CameraMove();
        NotifySelectionChanged();
    }

    public void ToNextTarget()
    {
        int groupCount = decorator != null ? decorator.GetGroupCount() : 0;
        if (groupCount == 0)
            return;

        _selectedGroupIndex = _selectedGroupIndex < groupCount - 1 ? _selectedGroupIndex + 1 : 0;
        SelectSavedSkinIndex();
        CameraMove();
        NotifySelectionChanged();
    }

    public void ToPrevTarget()
    {
        int groupCount = decorator != null ? decorator.GetGroupCount() : 0;
        if (groupCount == 0)
            return;

        _selectedGroupIndex = _selectedGroupIndex > 0 ? _selectedGroupIndex - 1 : groupCount - 1;
        SelectSavedSkinIndex();
        CameraMove();
        NotifySelectionChanged();
    }

    private void CameraMove()
    {
        if (previewCamera == null || decorator == null)
            return;

        Vector3 targetPosition = decorator.GetPreviewPosition(CurrentTarget, _selectedIndex);
        previewCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraZ);
        decorator.ScrollTo(CurrentTarget, _selectedIndex);
    }

    private void NotifySelectionChanged()
    {
        SkinConfig.SkinItem skin = CurrentSkin;
        if (skin == null)
            return;

        ESkinState state = storeController != null
            ? storeController.CheckSkinState(CurrentTarget, skin.id)
            : ESkinState.ForSale;

        OnChangeTarget?.Invoke(CurrentTarget);
        OnChangeHeaderSkin?.Invoke(CurrentTarget, skin.id, skin.DisplayName, skin.cost, state);
    }

    private void SelectSavedSkinIndex()
    {
        SkinConfig.SkinItem[] skins = GetCurrentSkins();
        if (skins.Length == 0)
        {
            _selectedIndex = 0;
            return;
        }

        int selectedId = DataController.Instance != null ? DataController.Instance.GetSelectedSkin(CurrentTarget) : skins[0].id;
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null && skins[i].id == selectedId)
            {
                _selectedIndex = i;
                return;
            }
        }

        _selectedIndex = 0;
    }

    private SkinConfig.SkinItem[] GetCurrentSkins()
    {
        return decorator != null ? decorator.GetSkins(CurrentTarget) : Array.Empty<SkinConfig.SkinItem>();
    }

    private SkinConfig.SkinItem GetCurrentSkin()
    {
        SkinConfig.SkinItem[] skins = GetCurrentSkins();
        if (skins.Length == 0)
            return null;

        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, skins.Length - 1);
        return skins[_selectedIndex];
    }

    private void HandleSelectedSkin(SkinTarget target, int id)
    {
        if (target == CurrentTarget)
            NotifySelectionChanged();
    }
}
