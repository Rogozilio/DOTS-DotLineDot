using Components;
using SystemGroups;
using Tags;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(DisableComponentsSystemGroup))]
    [UpdateAfter(typeof(ClearMergeComponentSystem))]
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

            var countIsMoveMouse = SystemAPI.QueryBuilder().WithAll<IsMouseMove>().Build().CalculateEntityCount();
            
            if(countIsMoveMouse > 0)
                state.Dependency = new ClearIsLastMoveInIsMouseMoveSystemJob().Schedule(state.Dependency);

            state.Dependency = new ClearEnableIsMouseMoveJob
            {
                input = input
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(IsMouseMove))]
        public partial struct ClearIsLastMoveInIsMouseMoveSystemJob : IJobEntity
        {
            public void Execute(ref IsMouseMove isMouseMove)
            {
                isMouseMove.isLastMove = false;
            }
        }

        [BurstCompile]
        private partial struct ClearEnableIsMouseMoveJob : IJobEntity
        {
            public InputDataComponent input;

            private void Execute(EnabledRefRW<IsMouseMove> mouseMove)
            {
                if (!input.isLeftMouseUp && !input.isRightMouseUp) return;
                mouseMove.ValueRW = false;
            }
        }
    }
}