using Components.DynamicBuffers;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class DynamicBufferAuthoring : MonoBehaviour
    {
        private class DynamicBufferAuthoringBaker : Baker<DynamicBufferAuthoring>
        {
            public override void Bake(DynamicBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<BlockElementBuffer>(entity);
            }
        }
    }
}