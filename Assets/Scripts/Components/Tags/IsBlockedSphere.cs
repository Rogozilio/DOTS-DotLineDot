using Components;
using Unity.Entities;

namespace Tags
{
    [WriteGroup(typeof(SpawnSphereComponent))]
    public struct IsBlockedSphere : IComponentData, IEnableableComponent
    {
    }
}