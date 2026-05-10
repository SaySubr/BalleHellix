using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PassDetector : MonoBehaviour
{
    
    private LevelGenerator levelGenerator;

    private void Start()
    {
        levelGenerator = LevelGenerator.instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            BallController ball = other.GetComponent<BallController>();
            if (ball != null)
            {
                ShatterFloor();
                ball.IncreaseCombo();
                GameManager.instance.AddScore(10 * ball.GetCombo());
            }
        }
    }



    IEnumerator ShatterSequence(Transform floor)
    {
        // Hide the original floor
        floor.GetComponent<MeshRenderer>().enabled = false;
        floor.GetComponent<MeshCollider>().enabled = false;

        int chunkCount = 3;
        int segmentsPerChunk = levelGenerator.segments / chunkCount;
        
        List<GameObject> chunks = new List<GameObject>();

        for (int i = 0; i < chunkCount; i++)
        {
            
            GameObject chunk = new GameObject("ShatterPiece");
            chunk.transform.position = floor.position;
            chunk.transform.rotation = floor.rotation;
            
            MeshFilter mf = chunk.AddComponent<MeshFilter>();
            MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
            mr.materials = floor.GetComponent<MeshRenderer>().materials;
            
            mf.mesh = levelGenerator.GenerateShatterMesh(i *  segmentsPerChunk, segmentsPerChunk);
            
            Rigidbody rb = chunk.AddComponent<Rigidbody>();
            rb.linearDamping = 8f;
            rb.angularDamping = 5f;
            rb.useGravity = true;

            Vector3 pushDir = (chunk.transform.position + chunk.transform.forward) - floor.position;
            pushDir.y = 0.1f;
            rb.AddForce(pushDir.normalized * 1.5f, ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.Range(-2,2), Random.Range(-2,2), Random.Range(-2,2)), ForceMode.Impulse);
            
            chunks.Add(chunk);
        }

        float elapsed = 0f;
        float duration = 0.3f;
        foreach (GameObject chunk in chunks)
        {
            Destroy(chunk,duration);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f,0f, elapsed / duration);

            foreach (GameObject chunk in chunks)
            {
                if (chunk == null) continue;
                MeshRenderer mr = chunk.GetComponent<MeshRenderer>();

                foreach (Material m in mr.materials)
                {
                    if (m.HasProperty("_BaseColor")) // URP
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }
                    else if (m.HasProperty("_Color")) // Built in renderer
                    {
                        Color c = m.color;
                        c.a = alpha;
                        m.color = c;
                    }
                }
            }

            yield return null;
        }

        if (floor != null) Destroy(floor.gameObject);
    }


    public void ShatterFloor()
    {
        StartCoroutine(ShatterSequence(transform.parent));
    }

}









