using Unity.Entities;

namespace Components.Shared
{
    public struct IndexSharedComponent : ISharedComponentData
    {
        public int value;
    }
}