using Unity.Entities;

namespace Components
{
    public struct SpawnSphereComponent : IComponentData
    {
        public int index;
        public bool isAddConnectSphere;
    }
}