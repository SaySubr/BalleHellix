using UnityEngine;

public class CameraInterpolate : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [Header("Тряска")]
    [SerializeField] private bool isShaking = true;
    [SerializeField] private float amplitudePower = 0.05f; 
    [SerializeField] private float shakingSpeed = 0.5f;   
    private Vector3 startLocalPos;
    private float seed;

    private void Start()
    {
        startLocalPos = _camera.transform.localPosition;
        seed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (!isShaking)
        {
            _camera.transform.localPosition = startLocalPos;
            return;
        }

        float time = Time.time * shakingSpeed;

        float x = (Mathf.PerlinNoise(seed, time) - 0.5f) * amplitudePower;
        float y = (Mathf.PerlinNoise(seed + 1f, time) - 0.5f) * amplitudePower;

        _camera.transform.localPosition = startLocalPos + new Vector3(x, y, 0f);
    }
}
