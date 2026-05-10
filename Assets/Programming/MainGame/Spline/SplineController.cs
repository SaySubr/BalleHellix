using UnityEngine;
using UnityEngine.Splines;

public class SplineController : MonoBehaviour
{
    [SerializeField] private SplineAnimate splineAnimate;

    [Header("Смена направления")]
    [SerializeField] private bool changeByTime = true;
    [SerializeField] private float minChangeTime = 2f;
    [SerializeField] private float maxChangeTime = 5f;

    [Header("Рандом")]
    [SerializeField] private bool randomDirection = true;

    private float timer;
    private float nextChangeTime;

    private void Start()
    {
        if (!splineAnimate)
            splineAnimate = GetComponent<SplineAnimate>();

        SetNextTime();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextChangeTime)
        {
            ChangeDirection();
            SetNextTime();
            timer = 0f;
        }
    }

    private void ChangeDirection()
    {
        
    }

    private void SetNextTime()
    {
        nextChangeTime = Random.Range(minChangeTime, maxChangeTime);
    }
}
