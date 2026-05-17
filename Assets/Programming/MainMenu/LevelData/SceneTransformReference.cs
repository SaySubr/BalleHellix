using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Config
{
    [Serializable]
    public class SceneTransformReference
    {
        [SerializeField] private string scenePath = "";
        [SerializeField] private string sceneObjectName = "";

        public string ScenePath => scenePath;
        public string SceneObjectName => sceneObjectName;
        public bool HasValue => !string.IsNullOrWhiteSpace(scenePath);

        public Transform Resolve()
        {
            return SceneTransformReferenceUtility.FindTransform(scenePath);
        }

        public void Set(Transform target)
        {
            if (target == null)
            {
                Clear();
                return;
            }

            scenePath = SceneTransformReferenceUtility.GetPath(target);
            sceneObjectName = target.name;
        }

        public void Clear()
        {
            scenePath = "";
            sceneObjectName = "";
        }
    }

    public static class SceneTransformReferenceUtility
    {
        public static string GetPath(Transform target)
        {
            if (target == null)
                return "";

            string path = target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        public static Transform FindTransform(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                return null;

            string[] parts = scenePath.Split('/');
            if (parts.Length == 0)
                return null;

            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root.name != parts[0])
                    continue;

                if (parts.Length == 1)
                    return root.transform;

                string childPath = string.Join("/", parts, 1, parts.Length - 1);
                return root.transform.Find(childPath);
            }

            return null;
        }
    }
}
