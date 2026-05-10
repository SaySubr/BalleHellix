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
    public Transform StartPoint => startPoint != null ? startPoint : transform;
    public float StepBySkins => stepBySkins;
    public float RowSpacing => rowSpacing;

    private readonly Dictionary<SkinTarget, Transform> _rowRoots = new Dictionary<SkinTarget, Transform>();

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
        Vector3 start = StartPoint.position;
        return new Vector3(start.x, start.y - rowSpacing * rowIndex, start.z);
    }

    public void ScrollTo(SkinTarget target, int skinIndex)
    {
        foreach (KeyValuePair<SkinTarget, Transform> row in _rowRoots)
        {
            if (row.Value == null)
                continue;

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

        Vector3 start = StartPoint.position;
        rowRoot.position = new Vector3(start.x, start.y - rowSpacing * rowIndex, start.z);
        rowRoot.localRotation = Quaternion.identity;
        rowRoot.localScale = Vector3.one;

        _rowRoots[target] = rowRoot;
        return rowRoot;
    }
}
