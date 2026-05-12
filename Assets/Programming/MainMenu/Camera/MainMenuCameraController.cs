using MainMenu.LevelMap;
using UnityEngine;

namespace MainMenu
{
    public class MainMenuCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float minZ = -50f;
        [SerializeField] private float maxZ = 50f;

        [Header("Click")]
        [SerializeField] private LayerMask islandLayer;
        [SerializeField] private float clickMaxDragDistance = 20f;
        [SerializeField] private bool ignoreInputOverUI = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private bool _isDragging;
        private bool _startedOverUI;
        private Vector2 _lastPointerPosition;
        private Vector2 _startPointerPosition;
        private Camera _mainCamera;
        private IslandSpawner _islandSpawner;

        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Start()
        {
            _islandSpawner = FindFirstObjectByType<IslandSpawner>();
            if (_islandSpawner == null && showDebugInfo)
                Debug.Log("MainMenuCameraController: IslandSpawner not found, islands will launch themselves.");
        }

        private void Update()
        {
            HandlePointer();
        }

        private void HandlePointer()
        {
            if (!ScreenPointerUtility.TryGetPrimaryPointer(out ScreenPointerState pointer))
            {
                _isDragging = false;
                _startedOverUI = false;
                return;
            }

            if (pointer.WasPressedThisFrame)
            {
                BeginPointer(pointer);
            }

            if (pointer.WasReleasedThisFrame)
            {
                EndPointer(pointer);
                return;
            }

            if (pointer.IsPressed)
                MoveByPointer(pointer.Position);
        }

        private void BeginPointer(ScreenPointerState pointer)
        {
            _isDragging = true;
            _startedOverUI = ignoreInputOverUI && ScreenPointerUtility.IsPointerOverUI(pointer.Position, pointer.PointerId);
            _startPointerPosition = pointer.Position;
            _lastPointerPosition = pointer.Position;
        }

        private void MoveByPointer(Vector2 currentPos)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _startPointerPosition = currentPos;
                _lastPointerPosition = currentPos;
                return;
            }

            if (_startedOverUI)
            {
                _lastPointerPosition = currentPos;
                return;
            }

            Vector2 delta = currentPos - _lastPointerPosition;
            _lastPointerPosition = currentPos;

            float moveInput = -delta.y * 0.01f * moveSpeed;
            if (Mathf.Approximately(moveInput, 0f))
                return;

            Vector3 newPos = transform.position + Vector3.forward * moveInput;
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
            transform.position = newPos;
        }

        private void EndPointer(ScreenPointerState pointer)
        {
            if (!_startedOverUI && Vector2.Distance(pointer.Position, _startPointerPosition) <= clickMaxDragDistance)
            {
                HandleRaycastClick(pointer.Position);
            }

            _isDragging = false;
            _startedOverUI = false;
        }

        private void HandleRaycastClick(Vector3 screenPosition)
        {
            if (_mainCamera == null)
                return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, islandLayer))
                return;

            Island island = hit.collider.GetComponentInParent<Island>();
            if (island != null)
            {
                island.Select();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(-100, 0, minZ), new Vector3(100, 0, minZ));
            Gizmos.DrawLine(new Vector3(-100, 0, maxZ), new Vector3(100, 0, maxZ));
        }
    }
}
