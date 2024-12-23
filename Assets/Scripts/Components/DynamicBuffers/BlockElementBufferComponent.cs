using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(0)]
    public struct BlockElementBufferComponent : IBufferElementData
    {
        public Entity element;
    }
}