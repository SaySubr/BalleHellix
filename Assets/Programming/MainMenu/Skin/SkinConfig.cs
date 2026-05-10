using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum SkinTarget
{
    HelixBall = 0,
    FireballTank = 1
}

[CreateAssetMenu(fileName = "SkinCatalogConfig", menuName = "Data/Skin Catalog Config", order = 51)]
public class SkinConfig : ScriptableObject
{
    [SerializeField] private SkinGroup[] groups =
    {
        new SkinGroup { target = SkinTarget.HelixBall, title = "Helix Ball" },
        new SkinGroup { target = SkinTarget.FireballTank, title = "Fireball Tank" }
    };

    public SkinGroup[] Groups => groups;

    public SkinItem[] GetSkins(SkinTarget target)
    {
        SkinGroup group = GetGroup(target);
        return group != null && group.skins != null ? group.skins : Array.Empty<SkinItem>();
    }

    public SkinGroup GetGroup(SkinTarget target)
    {
        if (groups == null)
            return null;

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].target == target)
                return groups[i];
        }

        return null;
    }

    public SkinItem GetSkin(SkinTarget target, int id)
    {
        SkinItem[] skins = GetSkins(target);
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null && skins[i].id == id)
                return skins[i];
        }

        return null;
    }

    public int GetDefaultSkinId(SkinTarget target)
    {
        SkinItem[] skins = GetSkins(target);
        if (skins.Length > 0 && skins[0] != null)
            return skins[0].id;

        return 1;
    }

    public int GetGroupIndex(SkinTarget target)
    {
        if (groups == null)
            return -1;

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].target == target)
                return i;
        }

        return -1;
    }

    public SkinTarget GetTargetByGroupIndex(int index)
    {
        if (groups == null || groups.Length == 0)
            return SkinTarget.HelixBall;

        index = Mathf.Clamp(index, 0, groups.Length - 1);
        return groups[index].target;
    }

    [Serializable]
    public class SkinGroup
    {
        public SkinTarget target;
        public string title;
        public SkinItem[] skins = Array.Empty<SkinItem>();
    }

    [Serializable]
    public class SkinItem
    {
        [Min(0)] public int id = 1;

        [FormerlySerializedAs("name")]
        public string displayName = "Skin";

        [Min(0)] public int cost = 0;
        public GameObject prefab;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"Skin {id}" : displayName;
    }
}
