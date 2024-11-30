using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
   
    public partial struct DisableIsMouseMoveSystem : ISystem
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
                if(!input.isMouseUp) return;
                
                mouseMove.ValueRW = false;
            }
        }
        
    }
}