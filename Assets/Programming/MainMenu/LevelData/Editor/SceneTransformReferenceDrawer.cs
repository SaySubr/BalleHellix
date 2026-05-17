using Config;
using UnityEditor;
using UnityEngine;

namespace MainMenu.LevelData.Editor
{
    [CustomPropertyDrawer(typeof(SceneTransformReference))]
    public class SceneTransformReferenceDrawer : PropertyDrawer
    {
        private const string ScenePathProperty = "scenePath";
        private const string SceneObjectNameProperty = "sceneObjectName";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty scenePath = property.FindPropertyRelative(ScenePathProperty);
            SerializedProperty sceneObjectName = property.FindPropertyRelative(SceneObjectNameProperty);

            GameObject current = ResolveObject(scenePath.stringValue);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            Object selected = EditorGUI.ObjectField(position, label, current, typeof(GameObject), true);

            if (EditorGUI.EndChangeCheck())
            {
                if (selected == null)
                {
                    scenePath.stringValue = "";
                    sceneObjectName.stringValue = "";
                }
                else if (selected is GameObject selectedObject && !EditorUtility.IsPersistent(selectedObject))
                {
                    scenePath.stringValue = SceneTransformReferenceUtility.GetPath(selectedObject.transform);
                    sceneObjectName.stringValue = selectedObject.name;
                }
                else
                {
                    Debug.LogWarning("SceneTransformReference: use a GameObject from the scene, not a prefab asset.");
                }
            }

            EditorGUI.EndProperty();
        }

        private static GameObject ResolveObject(string scenePath)
        {
            Transform target = SceneTransformReferenceUtility.FindTransform(scenePath);
            return target != null ? target.gameObject : null;
        }
    }
}
