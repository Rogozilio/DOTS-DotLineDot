using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class MultiSphereAuthoring : MonoBehaviour
    {
        public byte countElements;
        public GameObject prefabElement;
        public GameObject endSphere;
    }

    public class MultiSphereAuthoringBaker : Baker<MultiSphereAuthoring>
    {
        public override void Bake(MultiSphereAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var multiSphere = new MultiSphereComponent();
            multiSphere.countElements = authoring.countElements;
            multiSphere.prefabElement = GetEntity(authoring.prefabElement, TransformUsageFlags.Dynamic);
            multiSphere.endSphere = GetEntity(authoring.endSphere, TransformUsageFlags.Dynamic);
            
            AddComponent(entity, multiSphere);
            AddComponent(entity, new TagInitMultiSphere());
            AddComponent(entity, new IsMouseMove());
            SetComponentEnabled<IsMouseMove>(entity, false);
        }
    }
}