using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(0)]
    public struct BlockElementBuffer : IBufferElementData
    {
        public int index;
        public Entity element;
    }
}