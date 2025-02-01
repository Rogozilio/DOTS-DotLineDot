using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class SphereAuthoring : MonoBehaviour
    {
        public byte countElements;

        private class SphereAuthoringBaker : Baker<SphereAuthoring>
        {
            public override void Bake(SphereAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SphereComponent() { countElements = authoring.countElements });
                AddBuffer<IndexConnectionBuffer>(entity);
                AddComponent(entity, new IsMouseMove());
                AddComponent(entity, new IsBlockedSphere());
                AddComponent(entity, new IsCollisionWithSphere());

                SetComponentEnabled<IsMouseMove>(entity, false);
                SetComponentEnabled<IsBlockedSphere>(entity, false);
                SetComponentEnabled<IsCollisionWithSphere>(entity, false);

                AddSharedComponent(entity, new IndexSharedComponent { value = -1 });
            }
        }
    }
}