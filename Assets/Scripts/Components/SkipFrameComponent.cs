using Unity.Entities;

namespace Components
{
    public struct SkipFrameComponent : IComponentData
    {
        public byte count;
    }
}