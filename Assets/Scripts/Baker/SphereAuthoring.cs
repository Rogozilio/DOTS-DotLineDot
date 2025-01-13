using Components.DynamicBuffers;
using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class SphereAuthoring : MonoBehaviour
    {
        private class SphereAuthoringBaker : Baker<SphereAuthoring>
        {
            public override void Bake(SphereAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new IsMouseMove());
                AddComponent<TagSphere>(entity);
                AddBuffer<IndexConnectionBuffer>(entity);
                SetComponentEnabled<IsMouseMove>(entity, false);
            }
        }
    }
}