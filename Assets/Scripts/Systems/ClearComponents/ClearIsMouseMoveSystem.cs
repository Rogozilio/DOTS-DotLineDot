using Components;
using SystemGroups;
using Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(DisableComponentsSystemGroup))]
    public partial struct ClearIsMouseMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputDataComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<InputDataComponent>();
            
            state.Dependency = new DropTargetHitRaycastJob
            {
                input = input
            }.Schedule(state.Dependency);
            state.Dependency = new ClearVelocityAndRotateJob().Schedule(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct DropTargetHitRaycastJob : IJobEntity
        {
            public InputDataComponent input;
            
            private void Execute(EnabledRefRW<IsMouseMove> mouseMove)
            {
                if(!input.isLeftMouseUp && !input.isRightMouseUp) return;
                mouseMove.ValueRW = false;
            }
        }

        [BurstCompile]
        [WithAll(typeof(SphereComponent))]
        public partial struct ClearVelocityAndRotateJob : IJobEntity
        {
            public void Execute(ref PhysicsVelocity physicsVelocity)
            {
                physicsVelocity.Linear = float3.zero;
                physicsVelocity.Angular = float3.zero;
            }
        }
        
    }
}