using MainMenu.LevelMap;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private bool _isDragging;
        private Vector2 _lastPointerPosition;
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
            HandleMovement();
            HandleClick();
        }

        private void HandleMovement()
        {
            float moveInput = 0f;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    Vector2 currentPos = Mouse.current.position.ReadValue();
                    moveInput = ReadDragDelta(currentPos);
                }
                else
                {
                    _isDragging = false;
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
                moveInput = ReadDragDelta(currentPos);
            }

            if (moveInput != 0f)
            {
                Vector3 newPos = transform.position + Vector3.forward * moveInput;
                newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
                transform.position = newPos;
            }
        }

        private float ReadDragDelta(Vector2 currentPos)
        {
            float moveInput = 0f;

            if (_isDragging)
            {
                Vector2 delta = currentPos - _lastPointerPosition;
                moveInput = -delta.y * 0.01f * moveSpeed;
            }
            else
            {
                _isDragging = true;
            }

            _lastPointerPosition = currentPos;
            return moveInput;
        }

        private void HandleClick()
        {
            bool isClick = false;
            Vector3 clickPosition = Vector3.zero;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isClick = true;
                clickPosition = Mouse.current.position.ReadValue();
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                isClick = true;
                clickPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (isClick)
            {
                HandleRaycastClick(clickPosition);
            }
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
