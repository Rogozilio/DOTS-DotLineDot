using Unity.Entities;

namespace Components
{
    public struct MergeComponent : IComponentData, IEnableableComponent
    {
        public Entity target;
    }
}