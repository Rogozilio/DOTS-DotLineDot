using Components;
using SystemGroups;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
                input = input,
                
            }.Schedule(state.Dependency);
        }
        
        private partial struct DropTargetHitRaycastJob : IJobEntity
        {
            public InputDataComponent input;
            
            private void Execute(EnabledRefRW<IsMouseMove> mouseMove)
            {
                if(!input.isLeftMouseUp && !input.isRightMouseUp) return;
                mouseMove.ValueRW = false;
            }
        }
        
    }
}