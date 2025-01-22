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
                AddComponent<TagSphere>(entity);
                AddBuffer<IndexConnectionBuffer>(entity);
                AddComponent(entity, new IsMouseMove());
                AddComponent(entity, new IsCollisionWithSphere());

                SetComponentEnabled<IsMouseMove>(entity, false);
                SetComponentEnabled<IsCollisionWithSphere>(entity, false);
            }
        }
    }
}