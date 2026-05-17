using UnityEngine;

namespace MainMenu.LevelMap
{
    [DisallowMultipleComponent]
    public class IslandTargetPoint : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        [SerializeField] private float gizmoSize = 1f;

        private void OnDrawGizmos()
        {
            if (!drawGizmo)
                return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, gizmoSize));
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * Mathf.Max(0.25f, gizmoSize));
        }
    }
}
