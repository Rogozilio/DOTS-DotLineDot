using Unity.Entities;
using Unity.Entities.Serialization;

namespace Components.DynamicBuffers
{
    [InternalBufferCapacity(128)]
    public struct SceneBuffer : IBufferElementData
    {
        public SceneDataComponent value;
    }
}