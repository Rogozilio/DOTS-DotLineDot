using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Baker
{
    public class SpawnSphereAuthoring : MonoBehaviour
    {
        [HideInInspector] public int index = -1;
        [HideInInspector] public int countElements = 20;
        [HideInInspector] public bool blockMove;

        private void OnValidate()
        {
            if(index >= 0) return;
            
            var newIndex = -1;
            foreach (var spawnSphere in FindObjectsByType<SpawnSphereAuthoring>(FindObjectsSortMode.None))
            {
                if(spawnSphere == this) continue;

                newIndex = math.max(newIndex, spawnSphere.index);
            }

            index = newIndex + 1;
        }

        private class SpawnSphereAuthoringBaker : Baker<SpawnSphereAuthoring>
        {
            public override void Bake(SpawnSphereAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new SpawnSphereComponent
                {
                    index = authoring.index,
                    countElements = authoring.countElements,
                    isBlockMove = authoring.blockMove
                });
            }
        }

        [CustomEditor(typeof(SpawnSphereAuthoring))]
        private class SpawnSphereAuthoringEditor : Editor
        {
            private SerializedProperty _indexProperty;
            private SerializedProperty _countElementsProperty;
            private SerializedProperty _blockMoveMouse;
            private void OnEnable()
            {
                _indexProperty = serializedObject.FindProperty("index");
                _countElementsProperty = serializedObject.FindProperty("countElements");
                _blockMoveMouse = serializedObject.FindProperty("blockMove");
            }

            public override void OnInspectorGUI()
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(_indexProperty);
                GUI.enabled = true;
                
                EditorGUILayout.PropertyField(_countElementsProperty);
                EditorGUILayout.PropertyField(_blockMoveMouse);

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}