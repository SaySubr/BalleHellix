using UnityEngine;

[DisallowMultipleComponent]
public sealed class TowerBlockSlot : MonoBehaviour
{
    public int OriginalIndex { get; private set; }
    public int CurrentSlotIndex { get; private set; }
    public Vector3 CurrentSlotLocalPosition { get; private set; }

    private Transform towerRoot;

    public void Initialize(Transform root, int originalIndex, Vector3 slotLocalPosition)
    {
        towerRoot = root;
        OriginalIndex = originalIndex;
        MoveToSlot(originalIndex, slotLocalPosition);
    }

    public void MoveToSlot(int slotIndex, Vector3 slotLocalPosition)
    {
        CurrentSlotIndex = slotIndex;
        CurrentSlotLocalPosition = slotLocalPosition;

        if (towerRoot != null && transform.parent == towerRoot)
        {
            transform.localPosition = slotLocalPosition;
            return;
        }

        transform.position = towerRoot != null
            ? towerRoot.TransformPoint(slotLocalPosition)
            : slotLocalPosition;
    }
}
