using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct TargetGravityComponent : IComponentData, IEnableableComponent
    {
        public Entity target;
        public float3 position;
        public float distance;
    }
}