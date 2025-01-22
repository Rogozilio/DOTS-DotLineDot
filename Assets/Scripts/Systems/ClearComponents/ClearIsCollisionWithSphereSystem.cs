using Components;
using SystemGroups;
using Tags;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(DisableComponentsSystemGroup))]
    public partial struct ClearIsCollisionWithSphereSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ClearIsCollisionWithSphereComponentJob().Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct ClearIsCollisionWithSphereComponentJob : IJobEntity
        {
            public void Execute(EnabledRefRW<IsCollisionWithSphere> enabledIsCollisionWithSphere)
            {
                enabledIsCollisionWithSphere.ValueRW = false;
            }
        }
    }
}