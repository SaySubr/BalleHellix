using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Параметры оружия")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float fireRate = 0.15f; // 0.15 = ~7 выстрелов в секунду
    [SerializeField] private Transform shootPoint;

    public float Damage => damage;
    public float FireRate => fireRate;
    public Transform ShootPoint => shootPoint;

    private void Awake()
    {
        if (shootPoint == null)
            shootPoint = transform;
    }
}