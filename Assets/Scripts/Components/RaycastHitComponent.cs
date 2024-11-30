using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct RaycastHitComponent : IComponentData
    {
        public float3 position;
    }
}