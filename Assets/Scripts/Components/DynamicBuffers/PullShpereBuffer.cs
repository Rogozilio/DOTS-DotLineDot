using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(128)]
    public struct PullSphereBuffer : IBufferElementData
    {
        public Entity value;
    }
}