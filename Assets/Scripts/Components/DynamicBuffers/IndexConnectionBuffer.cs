using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(32)]
    public struct IndexConnectionBuffer : IBufferElementData
    {
        public int value;
    }
}