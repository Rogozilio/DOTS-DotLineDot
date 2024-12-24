using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class MultiSphereAuthoring : MonoBehaviour
    {
        public byte countElements;
        public GameObject prefabSphere;
        public GameObject prefabElement;
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
            multiSphere.prefabSphere = GetEntity(authoring.prefabSphere, TransformUsageFlags.Dynamic);
            multiSphere.prefabElement = GetEntity(authoring.prefabElement, TransformUsageFlags.Dynamic);
            multiSphere.speedMoveSphere = authoring.speedMoveSphere;
            multiSphere.speedGravityInSphere = authoring.speedGravityInSphere;
            
            AddComponent(entity, multiSphere);
        }
    }
}