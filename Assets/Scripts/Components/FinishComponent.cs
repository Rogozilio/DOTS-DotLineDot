using Unity.Entities;

namespace Components
{
    public struct FinishComponent : IComponentData, IEnableableComponent
    {
        public Entity sphere;
    }
}