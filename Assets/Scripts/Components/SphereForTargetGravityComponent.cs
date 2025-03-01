using Unity.Entities;

namespace Components
{
    public struct SphereForTargetGravityComponent : IComponentData
    {
        public Entity sphere;
    }
}