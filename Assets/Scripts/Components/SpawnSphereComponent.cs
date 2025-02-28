using Unity.Entities;

namespace Components
{
    public struct SpawnSphereComponent : IComponentData
    {
        public int index;
        public int countElements;
        public bool isBlockMove;
        public bool isAddConnectSphere;
    }
}