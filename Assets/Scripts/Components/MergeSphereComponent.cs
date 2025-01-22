using Unity.Entities;

namespace Components
{
    public struct MergeSphereComponent : IComponentData
    {
        public Entity from;
        public Entity to;
    }
}