using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using SceneUtility = Utilities.SceneUtility;

namespace Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct FinishLevelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneBuffer>();
            state.RequireForUpdate<FinishComponent>();
            state.RequireForUpdate<CurrentSceneComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            var countDisableFinishes = new NativeReference<int>(Allocator.TempJob)
            {
                Value = SystemAPI.QueryBuilder().WithPresent<FinishComponent>().Build().CalculateEntityCount()
            };

            state.Dependency = new MoveSphereIntoFinishJob
            {
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(true),
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                countDisableFinishes = countDisableFinishes
            }.Schedule(state.Dependency);
            state.Dependency = new CreateLoadLevelComponentJob
            {
                ecb = ecb,
                countDisableFinishes = countDisableFinishes,
                entityGlobalData = SystemAPI.GetSingletonEntity<SceneBuffer>(),
                sceneBuffer = SystemAPI.GetSingletonBuffer<SceneBuffer>(),
                currentScene = SystemAPI.GetSingleton<CurrentSceneComponent>()
            }.Schedule(state.Dependency);
            state.Dependency = new ClearFinishComponentJob().Schedule(state.Dependency);
            state.Dependency = countDisableFinishes.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithPresent(typeof(FinishComponent))]
        public partial struct MoveSphereIntoFinishJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;
            public ComponentLookup<LocalTransform> localTransforms;
            public NativeReference<int> countDisableFinishes;

            public void Execute(Entity entity, ref FinishComponent finish, EnabledRefRW<FinishComponent> finishEnabled)
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
                    finishEnabled.ValueRW = true;
                    countDisableFinishes.Value--;
                }

                localTransforms[finish.sphere] = transform;
            }
        }

        [BurstCompile]
        public struct CreateLoadLevelComponentJob : IJob
        {
            public EntityCommandBuffer ecb;
            public NativeReference<int> countDisableFinishes;
            public Entity entityGlobalData;
            [ReadOnly] public DynamicBuffer<SceneBuffer> sceneBuffer;
            [ReadOnly] public CurrentSceneComponent currentScene;
            public void Execute()
            {
                if(countDisableFinishes.Value > 0) return;
                
                ecb.AddComponent(entityGlobalData, new LoadLevelComponent()
                {
                    data = SceneUtility.NextLevel(sceneBuffer, currentScene.data)
                });
            }
        }

        [BurstCompile]
        [WithPresent(typeof(FinishComponent))]
        public partial struct ClearFinishComponentJob : IJobEntity
        {
            public void Execute(ref FinishComponent finish, EnabledRefRW<FinishComponent> finishEnabled)
            {
                finish.sphere = Entity.Null;
                finishEnabled.ValueRW = false;
            }
        }
    }
}