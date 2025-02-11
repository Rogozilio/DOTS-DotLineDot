using Components.DynamicBuffers;
using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class LevelSettingAuthoring : MonoBehaviour
    {
        [Header("Sphere")] 
        public int countSphere = 32;
        public int countElement = 256;
        public float speedMoveSphere = 1f;
        public float speedGravityInSphere = 1f;
        [Header("Prefabs")]
        public GameObject prefabSphere;
        public GameObject prefabElement;
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
            
            AddComponent(entity, levelSetting);
            AddBuffer<PullSphereBuffer>(entity);
            AddBuffer<PullElementBuffer>(entity);
            AddBuffer<PullJointBuffer>(entity);
        }
    }
}