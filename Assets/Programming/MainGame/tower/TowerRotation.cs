using UnityEngine;

/// <summary>
/// ????????? ???????? ????? ?????.
/// </summary>
public class TowerRotation : MonoBehaviour
{
    [Header("????????? ???????")]
    [SerializeField] private Vector3 startRotation;

    [Header("????????")]
    [SerializeField] private bool rotateAlways = true;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool invertRotation;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("?????????")]
    [SerializeField] private bool pulsate = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    private Vector3 originalScale;
    private float pulsePhase;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(startRotation);
        originalScale = transform.localScale;
        pulsePhase = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (rotateAlways)
        {
            float direction = invertRotation ? -1f : 1f;

            transform.Rotate(
                rotationAxis * rotationSpeed * direction * Time.deltaTime
            );
        }

        if (pulsate)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + pulsePhase) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }

    // ????????? ?????? ??? ?????????
    public void SetStartRotation(Vector3 rotation)
    {
        startRotation = rotation;
        transform.rotation = Quaternion.Euler(startRotation);
    }

    public void SetRotateAlways(bool enable)
    {
        rotateAlways = enable;
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public void SetInverted(bool invert)
    {
        invertRotation = invert;
    }

    public void SetRotationAxis(Vector3 axis)
    {
        rotationAxis = axis.normalized;
    }

    public void EnablePulsation(bool enable, float speed = 2f, float amount = 0.1f)
    {
        pulsate = enable;
        pulseSpeed = speed;
        pulseAmount = amount;
    }

    // ?????????????? ???????? ??????
    public void SetRandomRotation()
    {
        startRotation = new Vector3(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );
        transform.rotation = Quaternion.Euler(startRotation);
    }

    public void SetRandomSpeed(Vector2 range)
    {
        rotationSpeed = Random.Range(range.x, range.y);
    }

    public void RandomizeAll(Vector2 speedRange)
    {
        SetRandomRotation();
        SetRandomSpeed(speedRange);
        invertRotation = Random.value > 0.5f;

        // ????????? ??? ????????
        int axis = Random.Range(0, 3);
        switch (axis)
        {
            case 0: rotationAxis = Vector3.up; break;
            case 1: rotationAxis = Vector3.right; break;
            case 2: rotationAxis = Vector3.forward; break;
        }
    }
}
