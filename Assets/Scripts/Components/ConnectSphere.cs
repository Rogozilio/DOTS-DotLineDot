using Unity.Entities;

namespace Components
{
    public struct ConnectSphere : IComponentData
    {
        public Entity target;
    }
}