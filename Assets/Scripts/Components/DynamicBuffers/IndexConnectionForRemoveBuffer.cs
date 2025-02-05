using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(32)]
    public struct IndexConnectionForRemoveBuffer : IBufferElementData
    {
        public int value;
    }
}