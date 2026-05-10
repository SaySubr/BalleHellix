using UnityEngine;

public class CameraFollow : MonoBehaviour
{
   public Transform target;
   public Vector3 offset;
   public float smoothSpeed = 5f;

   void LateUpdate()
   {
      if (target == null) return;

      Vector3 desiredPos = target.position + offset;
      Vector3 smoothPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
      transform.position = smoothPos;
      
      transform.LookAt(target);
   }
}
