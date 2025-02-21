using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct FinishLevelSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveSphereIntoFinishJob
            {
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(true),
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
            }.Schedule(state.Dependency);
            state.Dependency = new ClearFinishComponentJob().Schedule(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public partial struct MoveSphereIntoFinishJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;
            public ComponentLookup<LocalTransform> localTransforms;

            public void Execute(Entity entity, ref FinishComponent finish)
            {
                if (finish.sphere == Entity.Null) return;

                if (isMouseMoves.IsComponentEnabled(finish.sphere)) return;

                float distance = math.distance(localTransforms[entity].Position,
                    localTransforms[finish.sphere].Position);
                
                float3 direction =
                    math.normalizesafe(localTransforms[entity].Position - localTransforms[finish.sphere].Position);

                var transform = localTransforms[finish.sphere];
                if (distance > 0.1f)
                    transform.Position += direction * 0.1f;
                else
                {
                    transform.Position = localTransforms[entity].Position;
                    transform.Rotation = quaternion.identity;
                    finish.isFinished = true;
                }

                localTransforms[finish.sphere] = transform;
            }
        }

        [BurstCompile]
        public partial struct ClearFinishComponentJob : IJobEntity
        {
            public void Execute(ref FinishComponent finish)
            {
                finish.sphere = Entity.Null;
                finish.isFinished = false;
            }
        }
    }
}