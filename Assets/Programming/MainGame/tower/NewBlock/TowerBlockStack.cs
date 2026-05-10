using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TowerBlockStack
{
    private readonly Transform root;
    private readonly List<Vector3> slotLocalPositions = new List<Vector3>();

    public TowerBlockStack(Transform root)
    {
        this.root = root;
    }

    public void Clear()
    {
        slotLocalPositions.Clear();
    }

    public Vector3 RegisterSlot(int index, float blockHeight)
    {
        Vector3 localPosition = Vector3.up * (index * blockHeight);

        while (slotLocalPositions.Count <= index)
            slotLocalPositions.Add(Vector3.zero);

        slotLocalPositions[index] = localPosition;
        return localPosition;
    }

    public void RegisterBlock(GameObject block, int index, float blockHeight)
    {
        if (block == null)
            return;

        Vector3 slotPosition = RegisterSlot(index, blockHeight);
        TowerBlockSlot slot = block.GetComponent<TowerBlockSlot>();
        if (slot == null)
            slot = block.AddComponent<TowerBlockSlot>();

        slot.Initialize(root, index, slotPosition);
    }

    public void Compact(IList<TowerBlock> blocks)
    {
        if (blocks == null || blocks.Count == 0)
            return;

        List<TowerBlockSlot> sortedSlots = blocks
            .Where(block => block != null && !block.IsDestroyed())
            .Select(block => block.GetComponent<TowerBlockSlot>())
            .Where(slot => slot != null)
            .OrderBy(slot => slot.OriginalIndex)
            .ToList();

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            Vector3 targetSlot = GetSlotPosition(i);
            sortedSlots[i].MoveToSlot(i, targetSlot);
        }
    }

    private Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < slotLocalPositions.Count)
            return slotLocalPositions[index];

        if (slotLocalPositions.Count > 1)
        {
            Vector3 step = slotLocalPositions[1] - slotLocalPositions[0];
            return slotLocalPositions[0] + step * index;
        }

        return Vector3.zero;
    }
}
