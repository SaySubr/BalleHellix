using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinDecorator : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private SkinConfig skinConfig;

    [Header("Preview Layout")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private float stepBySkins = 4f;
    [SerializeField] private float rowSpacing = 4f;

    public SkinConfig SkinConfig => skinConfig;
    public Transform StartPoint => ResolveStartPoint();
    public float StepBySkins => stepBySkins;
    public float RowSpacing => rowSpacing;
    public bool ShouldMovePreviewCamera => !ShouldUseFixedCameraTarget();

    private readonly Dictionary<SkinTarget, Transform> _rowRoots = new Dictionary<SkinTarget, Transform>();
    private readonly Dictionary<SkinTarget, List<SkinPreview>> _previews = new Dictionary<SkinTarget, List<SkinPreview>>();
    private bool _missingPreviewTargetLogged;

    private void Awake()
    {
        StartCoroutine(CreateSkinRoutine());
    }

    public SkinConfig.SkinItem[] GetSkins(SkinTarget target)
    {
        return skinConfig != null ? skinConfig.GetSkins(target) : System.Array.Empty<SkinConfig.SkinItem>();
    }

    public int GetGroupCount()
    {
        return skinConfig != null && skinConfig.Groups != null ? skinConfig.Groups.Length : 0;
    }

    public SkinTarget GetTargetByGroupIndex(int groupIndex)
    {
        return skinConfig != null ? skinConfig.GetTargetByGroupIndex(groupIndex) : SkinTarget.HelixBall;
    }

    public int GetGroupIndex(SkinTarget target)
    {
        return skinConfig != null ? skinConfig.GetGroupIndex(target) : 0;
    }

    public Vector3 GetPreviewPosition(SkinTarget target, int skinIndex)
    {
        int rowIndex = Mathf.Max(0, GetGroupIndex(target));
        Transform placementRoot = StartPoint;
        Vector3 start = placementRoot.position;
        return new Vector3(start.x, start.y - rowSpacing * rowIndex, start.z);
    }

    public void ScrollTo(SkinTarget target, int skinIndex)
    {
        bool useFixedCameraTarget = ShouldUseFixedCameraTarget();
        Transform previewTarget = useFixedCameraTarget ? StartPoint : null;

        if (useFixedCameraTarget)
        {
            PlaceSelectedPreviewAtTarget(target, skinIndex, previewTarget);
            return;
        }

        foreach (KeyValuePair<SkinTarget, Transform> row in _rowRoots)
        {
            if (row.Value == null)
                continue;

            row.Value.gameObject.SetActive(true);
            RestoreRowPreviewLayout(row.Key);

            Vector3 localPosition = row.Value.localPosition;
            localPosition.x = row.Key == target ? -stepBySkins * skinIndex : 0f;
            row.Value.localPosition = localPosition;
        }
    }

    private IEnumerator CreateSkinRoutine()
    {
        if (skinConfig == null || skinConfig.Groups == null)
            yield break;

        for (int row = 0; row < skinConfig.Groups.Length; row++)
        {
            SkinConfig.SkinGroup group = skinConfig.Groups[row];
            if (group == null || group.skins == null)
                continue;

            Transform rowRoot = GetOrCreateRowRoot(group.target, row);

            for (int i = 0; i < group.skins.Length; i++)
            {
                SkinConfig.SkinItem skin = group.skins[i];
                if (skin == null || skin.prefab == null)
                    continue;

                if (i != 0 && i % 10 == 0)
                    yield return null;

                GameObject preview = Instantiate(skin.prefab, rowRoot);
                preview.name = $"Preview_{group.target}_{skin.id}";
                preview.transform.localPosition = new Vector3(stepBySkins * i, 0f, 0f);
                preview.transform.localRotation = Quaternion.identity;
                RegisterPreview(group.target, preview.transform);
            }
        }
    }

    private Transform GetOrCreateRowRoot(SkinTarget target, int rowIndex)
    {
        if (_rowRoots.TryGetValue(target, out Transform rowRoot) && rowRoot != null)
            return rowRoot;

        GameObject rowObject = new GameObject($"SkinRow_{target}");
        rowRoot = rowObject.transform;
        rowRoot.SetParent(transform);

        Transform placementRoot = StartPoint;
        Vector3 start = placementRoot.position;
        rowRoot.position = new Vector3(start.x, start.y - rowSpacing * rowIndex, start.z);
        rowRoot.rotation = skinConfig != null && skinConfig.UsePreviewTargetRotation
            ? placementRoot.rotation
            : Quaternion.identity;

        if (skinConfig != null && skinConfig.UsePreviewTargetScale)
        {
            ApplyWorldScale(rowRoot, placementRoot.lossyScale);
        }
        else
        {
            rowRoot.localScale = Vector3.one;
        }

        _rowRoots[target] = rowRoot;
        return rowRoot;
    }

    private Transform ResolveStartPoint()
    {
        if (skinConfig != null && skinConfig.PreviewTargetPoint != null && skinConfig.PreviewTargetPoint.HasValue)
        {
            if (skinConfig.TryGetPreviewTargetPoint(out Transform targetPoint))
            {
                return targetPoint;
            }

            if (!_missingPreviewTargetLogged)
            {
                Debug.LogWarning($"SkinDecorator: preview target point '{skinConfig.PreviewTargetPoint.ScenePath}' was not found. Scene Start Point will be used.");
                _missingPreviewTargetLogged = true;
            }
        }

        return startPoint != null ? startPoint : transform;
    }

    private bool ShouldUseFixedCameraTarget()
    {
        return skinConfig != null
               && skinConfig.KeepPreviewCameraFixed
               && skinConfig.PreviewTargetPoint != null
               && skinConfig.PreviewTargetPoint.HasValue
               && skinConfig.TryGetPreviewTargetPoint(out _);
    }

    private void PlaceSelectedPreviewAtTarget(SkinTarget target, int skinIndex, Transform previewTarget)
    {
        if (previewTarget == null)
            return;

        foreach (KeyValuePair<SkinTarget, List<SkinPreview>> row in _previews)
        {
            bool isActiveTarget = row.Key == target;
            List<SkinPreview> previews = row.Value;

            if (previews == null)
                continue;

            for (int i = 0; i < previews.Count; i++)
            {
                SkinPreview preview = previews[i];
                if (preview == null || preview.Transform == null)
                    continue;

                bool isSelected = isActiveTarget && i == skinIndex;
                preview.Transform.gameObject.SetActive(isSelected);

                if (isSelected)
                {
                    PlacePreviewAtTarget(preview, previewTarget);
                }
            }
        }
    }

    private void PlacePreviewAtTarget(SkinPreview preview, Transform previewTarget)
    {
        Quaternion rotation = skinConfig != null && skinConfig.UsePreviewTargetRotation
            ? previewTarget.rotation
            : Quaternion.identity;

        preview.Transform.SetPositionAndRotation(previewTarget.position, rotation);

        if (skinConfig != null && skinConfig.UsePreviewTargetScale)
        {
            ApplyWorldScale(preview.Transform, previewTarget.lossyScale);
        }
        else
        {
            preview.Transform.localScale = preview.InitialLocalScale;
        }
    }

    private void RegisterPreview(SkinTarget target, Transform preview)
    {
        if (!_previews.TryGetValue(target, out List<SkinPreview> previews))
        {
            previews = new List<SkinPreview>();
            _previews[target] = previews;
        }

        previews.Add(new SkinPreview(preview));
    }

    private void RestoreRowPreviewLayout(SkinTarget target)
    {
        if (!_previews.TryGetValue(target, out List<SkinPreview> previews) || previews == null)
            return;

        for (int i = 0; i < previews.Count; i++)
        {
            SkinPreview preview = previews[i];
            if (preview == null || preview.Transform == null)
                continue;

            preview.Transform.gameObject.SetActive(true);
            preview.Restore();
        }
    }

    private static void ApplyWorldScale(Transform target, Vector3 worldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            DivideScale(worldScale.x, parentScale.x),
            DivideScale(worldScale.y, parentScale.y),
            DivideScale(worldScale.z, parentScale.z));
    }

    private static float DivideScale(float value, float parentScale)
    {
        return Mathf.Approximately(parentScale, 0f) ? value : value / parentScale;
    }

    private class SkinPreview
    {
        public SkinPreview(Transform transform)
        {
            Transform = transform;
            InitialLocalPosition = transform != null ? transform.localPosition : Vector3.zero;
            InitialLocalRotation = transform != null ? transform.localRotation : Quaternion.identity;
            InitialLocalScale = transform != null ? transform.localScale : Vector3.one;
        }

        public Transform Transform { get; }
        public Vector3 InitialLocalPosition { get; }
        public Quaternion InitialLocalRotation { get; }
        public Vector3 InitialLocalScale { get; }

        public void Restore()
        {
            if (Transform == null)
                return;

            Transform.localPosition = InitialLocalPosition;
            Transform.localRotation = InitialLocalRotation;
            Transform.localScale = InitialLocalScale;
        }
    }
}
