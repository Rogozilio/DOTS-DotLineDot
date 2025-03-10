using Components;
using SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(DisableComponentsSystemGroup))]
    public partial struct ClearMergeComponentSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ClearMergeComponentJob().Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(MergeComponent))]
        public partial struct ClearMergeComponentJob : IJobEntity
        {
            public void Execute(ref MergeComponent enabledMergeComponent)
            {
                enabledMergeComponent.target = Entity.Null;
            }
        }
    }
}