using Unity.Entities;

namespace SystemGroups
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DisableComponentsSystemGroup : ComponentSystemGroup
    {
    
    }
}