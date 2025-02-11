using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(256)]
    public struct PullElementBuffer : IBufferElementData
    {
        public Entity value;
    }
}