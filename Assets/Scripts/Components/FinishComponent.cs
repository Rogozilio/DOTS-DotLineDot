using Unity.Entities;

namespace Components
{
    public struct FinishComponent : IComponentData
    {
        public Entity sphere;
        public bool isFinished;
    }
}