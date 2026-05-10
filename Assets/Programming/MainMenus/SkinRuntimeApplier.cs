using Data;
using UnityEngine;

public class SkinRuntimeApplier : MonoBehaviour
{
    [SerializeField] private SkinTarget target = SkinTarget.HelixBall;
    [SerializeField] private SkinConfig skinConfig;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool hideOriginalRenderers = true;

    private GameObject _spawnedSkin;

    private void Start()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        ApplySelectedSkinTo(gameObject, target, skinConfig, visualRoot, hideOriginalRenderers, ref _spawnedSkin);
    }

    public static void ApplySelectedSkinTo(
        GameObject targetObject,
        SkinTarget target,
        SkinConfig skinConfig,
        Transform visualRoot,
        bool hideOriginalRenderers)
    {
        GameObject spawnedSkin = null;
        ApplySelectedSkinTo(targetObject, target, skinConfig, visualRoot, hideOriginalRenderers, ref spawnedSkin);
    }

    private static void ApplySelectedSkinTo(
        GameObject targetObject,
        SkinTarget target,
        SkinConfig skinConfig,
        Transform visualRoot,
        bool hideOriginalRenderers,
        ref GameObject spawnedSkin)
    {
        if (targetObject == null || skinConfig == null || DataController.Instance == null)
            return;

        int selectedId = DataController.Instance.GetSelectedSkin(target);
        SkinConfig.SkinItem skin = skinConfig.GetSkin(target, selectedId);
        if (skin == null || skin.prefab == null)
            return;

        if (spawnedSkin != null)
            Object.Destroy(spawnedSkin);

        Transform parent = visualRoot != null ? visualRoot : targetObject.transform;
        spawnedSkin = Object.Instantiate(skin.prefab, parent);
        spawnedSkin.name = $"SelectedSkin_{target}_{selectedId}";
        spawnedSkin.transform.localPosition = Vector3.zero;
        spawnedSkin.transform.localRotation = Quaternion.identity;
        spawnedSkin.transform.localScale = Vector3.one;
        DisableVisualPrefabPhysics(spawnedSkin);

        if (hideOriginalRenderers)
            SetOriginalRenderersEnabled(targetObject, spawnedSkin, false);
    }

    private static void DisableVisualPrefabPhysics(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }
    }

    private static void SetOriginalRenderersEnabled(GameObject targetObject, GameObject spawnedSkin, bool enabled)
    {
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (spawnedSkin != null && renderers[i].transform.IsChildOf(spawnedSkin.transform))
                continue;

            renderers[i].enabled = enabled;
        }
    }
}
