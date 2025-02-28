using Unity.Entities;
using Unity.Physics;

namespace Tags
{
    [WriteGroup(typeof(PhysicsVelocity))]
    [WriteGroup(typeof(PhysicsMass))]
    public struct BlockMoveSphereTag : IComponentData
    {
    }
}