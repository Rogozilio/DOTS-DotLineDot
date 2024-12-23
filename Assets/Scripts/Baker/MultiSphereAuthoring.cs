using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class MultiSphereAuthoring : MonoBehaviour
    {
        public byte countElements;
        public GameObject prefabElement;
        public GameObject startSphere;
        public GameObject endSphere;
        public float speedMoveSphere = 1f;
        public float speedGravityInSphere = 1f;
    }

    public class MultiSphereAuthoringBaker : Baker<MultiSphereAuthoring>
    {
        public override void Bake(MultiSphereAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var multiSphere = new MultiSphereComponent();
            multiSphere.countElements = authoring.countElements;
            multiSphere.prefabElement = GetEntity(authoring.prefabElement, TransformUsageFlags.Dynamic);
            multiSphere.startSphere = GetEntity(authoring.startSphere, TransformUsageFlags.Dynamic);
            multiSphere.endSphere = GetEntity(authoring.endSphere, TransformUsageFlags.Dynamic);
            multiSphere.speedMoveSphere = authoring.speedMoveSphere;
            multiSphere.speedGravityInSphere = authoring.speedGravityInSphere;
            
            AddComponent(entity, multiSphere);
            AddComponent(entity, new TagInitMultiSphere());
        }
    }
}