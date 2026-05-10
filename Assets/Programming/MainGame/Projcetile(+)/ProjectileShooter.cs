using UnityEngine;

/// <summary>
/// ????????? ???????? ?????????. ?????????? ??? ???? ? ??????.
/// </summary>
public class ProjectileShooter : MonoBehaviour, IShooter
{
    [Header("??????????")]
    [Tooltip("??? ????")]
    [SerializeField] private BulletPool bulletPool;
    [Tooltip("????????? ??????")]
    [SerializeField] private Weapon weapon;

    [Header("????????? ????????")]
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private LayerMask aimMask = -1;

    private float nextFireTime;
    private Transform shootPoint;

    private void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<Weapon>();

        if (bulletPool == null)
            bulletPool = GetComponent<BulletPool>();

        if (weapon != null)
            shootPoint = weapon.ShootPoint;
        else
            shootPoint = transform;
    }

    public void Shoot()
    {
        if (Time.time < nextFireTime) return;

        if (bulletPool == null || shootPoint == null || weapon == null)
        {
            Debug.LogError("ProjectileShooter: Missing components!");
            return;
        }

        Bullet bullet = bulletPool.GetBullet();
        if (bullet == null) return;

        bullet.transform.position = shootPoint.position;
        bullet.transform.rotation = shootPoint.rotation;
        bullet.Init(weapon.Damage, shootPoint.forward, bulletPool);

        nextFireTime = Time.time + weapon.FireRate;
    }
}
