using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(256)]
    public struct PullJointBuffer : IBufferElementData
    {
        public Entity value;
    }
}