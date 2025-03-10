using Unity.Entities;

namespace Components
{
    public struct ReconnectSphereComponent : IComponentData
    {
        public Entity from;
        public Entity to;
    }
}