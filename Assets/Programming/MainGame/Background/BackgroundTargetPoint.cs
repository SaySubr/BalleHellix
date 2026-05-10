using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BackgroundTargetPoint : MonoBehaviour
{
    public static event Action<BackgroundTargetPoint> Enabled;
    public static event Action<BackgroundTargetPoint> Disabled;

    [SerializeField] private Color gizmoColor = new Color(0.1f, 0.8f, 1f, 0.8f);
    [SerializeField] private float gizmoRadius = 0.75f;

    public Transform Point => transform;

    private void OnEnable()
    {
        Enabled?.Invoke(this);
    }

    private void OnDisable()
    {
        Disabled?.Invoke(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * (gizmoRadius * 2f));
    }
}
