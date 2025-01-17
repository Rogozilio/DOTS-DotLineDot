using Unity.Entities;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(128)]
    public struct NotActiveSphereBuffer : IBufferElementData
    {
        public Entity value;
    }
}