using Baker;
using Components;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EventCreateAndConnectElementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConnectSphere>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var levelSettings = SystemAPI.GetSingleton<LevelSettingComponent>();
            var elementsNotConnected = SystemAPI.QueryBuilder().WithAll<IsElementNotConnected>().Build();
            var entityElementsNotConnected = elementsNotConnected.ToEntityArray(Allocator.TempJob);

            state.Dependency = new CreateElementsJob
            {
                ecb = ecb,
                levelSettings = levelSettings,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true)
            }.Schedule(state.Dependency);
            if (entityElementsNotConnected.Length == 0)
            {
                entityElementsNotConnected.Dispose();
                return;
            }
            state.Dependency = new CreateJointElementsJob
            {
                ecb = ecb,
                elements = entityElementsNotConnected,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                levelSettings = levelSettings
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
            state.Dependency = new IncrementIndexConnectionJob().Schedule(state.Dependency);
            state.Dependency = new RemoveComponentIsElementNotConnectedJob()
            {
                ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            entityElementsNotConnected.Dispose();
        }

        [WithNone(typeof(TagElementsCreated))]
        private partial struct CreateElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public LevelSettingComponent levelSettings;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransforms;

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                for (byte i = 0; i < levelSettings.countElements; i++)
                {
                    var newElement = ecb.Instantiate(levelSettings.prefabElement);
                    ecb.SetName(newElement, "Element " + levelSettings.indexConnection + " (" + i + ")");
                    ecb.SetComponent(newElement, new IndexConnectComponent()
                    {
                        value = levelSettings.indexConnection
                    });
                    ecb.SetComponent(newElement, new LocalTransform()
                    {
                        Position = localTransforms[entity].Position,
                        Rotation = localTransforms[levelSettings.prefabElement].Rotation,
                        Scale = localTransforms[levelSettings.prefabElement].Scale
                    });
                }

                ecb.AddComponent<TagElementsCreated>(entity);
            }
        }

        private partial struct CreateJointElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public NativeArray<Entity> elements;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransforms;
            [ReadOnly] public LevelSettingComponent levelSettings;

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                var maxDistance = localTransforms[elements[0]].Scale / 1.5f;

                StaticMethod.CreateJoint(ecb, entity, elements[0], maxDistance, levelSettings.indexConnection);

                for (var i = 0; i < elements.Length - 1; i++)
                {
                    StaticMethod.CreateJoint(ecb, elements[i], elements[i + 1], maxDistance, levelSettings.indexConnection);
                }

                StaticMethod.CreateJoint(ecb, elements[^1], connectSphere.target, maxDistance, levelSettings.indexConnection);

                ecb.RemoveComponent<ConnectSphere>(entity);
                ecb.RemoveComponent<TagElementsCreated>(entity);
            }
            
        }
        
        private partial struct IncrementIndexConnectionJob : IJobEntity
        {
            private void Execute(ref LevelSettingComponent levelSettings)
            {
                levelSettings.indexConnection++;
            }
        }

        private partial struct RemoveComponentIsElementNotConnectedJob : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in IsElementNotConnected isElementNotConnected)
            {
                ecb.RemoveComponent<IsElementNotConnected>(sortKey, entity);
            }
        }
    }
}