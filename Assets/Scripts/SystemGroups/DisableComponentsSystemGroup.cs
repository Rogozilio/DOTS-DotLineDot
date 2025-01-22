using Unity.Entities;

namespace SystemGroups
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderFirst = true)]
    public partial class DisableComponentsSystemGroup : ComponentSystemGroup
    {
    
    }
}