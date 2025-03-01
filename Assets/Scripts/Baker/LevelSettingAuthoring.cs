using System;
using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Baker
{
    public class LevelSettingAuthoring : MonoBehaviour
    {
        [Header("Pull")] 
        public int countSphere = 32;
        public int countElement = 256;
        [Header("Sphere")] 
        public float speedMoveSphere = 1f;
        public float speedGravityInSphere = 1f;
        [Header("Prefabs")] 
        public GameObject prefabSphere;
        public GameObject prefabElement;
        [Header("Dynamic Spawn/remove")] 
        public float distanceRemove = 0.1f;
        public float distanceSpawn = 0.2f;
        public float distanceBetweenElements = 0.2f;

        private void OnValidate()
        {
            distanceSpawn = math.min(distanceSpawn, distanceBetweenElements);
        }
    }

    public class LevelSettingAuthoringBaker : Baker<LevelSettingAuthoring>
    {
        public override void Bake(LevelSettingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var levelSetting = new LevelSettingComponent();
            levelSetting.countElement = authoring.countElement;
            levelSetting.countSphere = authoring.countSphere;
            levelSetting.prefabSphere = GetEntity(authoring.prefabSphere, TransformUsageFlags.Dynamic);
            levelSetting.prefabElement = GetEntity(authoring.prefabElement, TransformUsageFlags.Dynamic);
            levelSetting.speedMoveSphere = authoring.speedMoveSphere;
            levelSetting.speedGravityInSphere = authoring.speedGravityInSphere;
            levelSetting.distanceRemove = authoring.distanceRemove;
            levelSetting.distanceSpawn = authoring.distanceSpawn;
            levelSetting.distanceBetweenElements = authoring.distanceBetweenElements;

            AddComponent(entity, levelSetting);
            AddBuffer<PullSphereBuffer>(entity);
            AddBuffer<PullElementBuffer>(entity);
            AddBuffer<PullJointBuffer>(entity);
            AddComponent<RemoveElementComponent>(entity);
            AddComponent<SphereForTargetGravityComponent>(entity);
        }
    }
}