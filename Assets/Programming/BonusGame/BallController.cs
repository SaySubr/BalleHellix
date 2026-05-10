using System;
using UnityEngine;
using Random = System.Random;

public class BallController : MonoBehaviour
{
    [Header("Physics settings")] 
    public float bounceForce = 12f;
    public float maxVelocity = 10f; // max speed while falling
    
    private Rigidbody rb;
    public GameObject splashPrefab;

    [Header("Skin")]
    [SerializeField] private SkinConfig skinConfig;
    [SerializeField] private Transform skinVisualRoot;
    [SerializeField] private bool hideOriginalRenderersWhenSkinPrefabExists = true;

    public bool isSmashing = false;
    private Vector3 originalScale;
    public float squashSpeed = 10f;

    private int comboMultiplier = 1;

    private void Start()
    {
        SkinRuntimeApplier.ApplySelectedSkinTo(gameObject, SkinTarget.HelixBall, skinConfig, skinVisualRoot, hideOriginalRenderersWhenSkinPrefabExists);

        rb = GetComponent<Rigidbody>();
        
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
        }
        
        isSmashing = rb.linearVelocity.magnitude >= maxVelocity;

        transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * squashSpeed);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            HandlePlatformCollision(collision);
        }
    }


    private void HandlePlatformCollision(Collision collision)
    {
       RaycastHit hit;
       Vector3 rayDir = collision.contacts[0].point - transform.position;

       if (Physics.Raycast(transform.position, rayDir.normalized, out hit, 2f))
       {
           MeshCollider meshCollider = hit.collider as MeshCollider;
           if (meshCollider == null || meshCollider.sharedMesh == null) return;

           
           int submeshIndex = GetSubMeshFromTriangle(meshCollider.sharedMesh, hit.triangleIndex);

           if (submeshIndex == 0)
           {
               Bounce(collision.contacts[0].point, collision.transform);
               if (isSmashing)
               {
                   collision.gameObject.GetComponentInChildren<PassDetector>().ShatterFloor();
               }
           } // hit safe
           else // hit danger
           {
               if (isSmashing)
               {
                   collision.gameObject.GetComponentInChildren<PassDetector>().ShatterFloor();
               }
               else
               {
                  Debug.Log("Game over");
                  GameManager.instance.GameOver();
               }
           }
       }
    }

    private void Bounce(Vector3 contactPoint, Transform platformTransform)
    {
        rb.linearVelocity = new Vector3(0, bounceForce, 0);
        
        transform.localScale = new Vector3(originalScale.x*1.4f,originalScale.y*0.6f,originalScale.z*1.4f);
        
        CreateEffects(contactPoint,platformTransform);
        comboMultiplier = 1;
    }

    public void IncreaseCombo()
    {
        comboMultiplier++;
    }

    public int GetCombo()
    {
        return comboMultiplier;
    }
  


    private int GetSubMeshFromTriangle(Mesh mesh, int triangleIndex)
    {

        int triangleStart = triangleIndex * 3;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int [] submeshTris = mesh.GetTriangles(i);

            for (int j = 0; j < submeshTris.Length; j++)
            {
                if (submeshTris[j] == mesh.triangles[triangleStart])
                {
                    return i;
                }
            }
        }

        return 0;
    }

    private void CreateEffects(Vector3 position, Transform parentFloor)
    {
        if (splashPrefab != null)
        {
            Vector3 spawnPos = new Vector3(position.x, position.y + 0.02f, position.z);
            GameObject splash = Instantiate(splashPrefab,spawnPos,Quaternion.Euler(90,UnityEngine.Random.Range(0,360f),0));
            
            splash.transform.SetParent(parentFloor);
        }        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            Debug.Log("Level Complete");
            GameManager.instance.LevelComplete();
        }
    }
}
