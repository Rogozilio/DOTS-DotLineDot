using Baker;
using Components;
using Components.DynamicBuffers;
using Components.Shared;
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
    public partial struct EventCreateAndConnectElementsSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConnectSphere>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var levelSettings = SystemAPI.GetSingletonRW<LevelSettingComponent>();
            var querySphere = SystemAPI.QueryBuilder().WithAll<SphereComponent, IndexSharedComponent>().Build();

            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<IndexSharedComponent> indexes,
                Allocator.Temp);

            levelSettings.ValueRW.maxCountELements = 0;

            foreach (var index in indexes)
            {
                if (index.value == -1) continue;

                querySphere.SetSharedComponentFilter(index);

                var spheres = querySphere.ToComponentDataArray<SphereComponent>(Allocator.Temp);

                if (spheres.Length != 0)
                {
                    levelSettings.ValueRW.maxCountELements += spheres[0].countElements;
                }

                spheres.Dispose();
            }

            indexes.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var levelSettings = SystemAPI.GetSingleton<LevelSettingComponent>();
            var elementsFromBuffer = new NativeArray<Entity>(levelSettings.maxCountELements, Allocator.TempJob);

            state.Dependency = new CreateElementsJob
            {
                ecb = ecb,
                elements = elementsFromBuffer,
                levelSettings = levelSettings,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                elementBuffer = SystemAPI.GetSingletonBuffer<PullElementBuffer>()
            }.Schedule(state.Dependency);
            state.Dependency = new CreateJointElementsJob
            {
                ecb = ecb,
                elements = elementsFromBuffer,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                levelSettings = levelSettings,
                jointBuffer = SystemAPI.GetSingletonBuffer<PullJointBuffer>()
            }.Schedule(state.Dependency);
            
            state.Dependency.Complete();
            state.Dependency = new IncrementIndexConnectionJob().Schedule(state.Dependency);
            
            elementsFromBuffer.Dispose();
        }

        [WithAll(typeof(ConnectSphere))]
        private partial struct CreateElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public LevelSettingComponent levelSettings;
            public NativeArray<Entity> elements;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransforms;
            public DynamicBuffer<PullElementBuffer> elementBuffer;

            private void Execute(Entity entity, in SphereComponent sphere, in IndexSharedComponent index)
            {
                for (byte i = 0; i < levelSettings.maxCountELements; i++)
                {
                    var newTransform = new LocalTransform
                    {
                        Position = localTransforms[entity].Position,
                        Rotation = localTransforms[levelSettings.prefabElement].Rotation,
                        Scale = localTransforms[levelSettings.prefabElement].Scale
                    };
                    var name = "Element " + levelSettings.indexConnection + " (" + i + ")";
                    var newElement = StaticMethod.CreateElement(ecb, elementBuffer, newTransform, levelSettings.indexConnection, index, name);
                    elements[i] = newElement;
                }
            }
        }

        private partial struct CreateJointElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public NativeArray<Entity> elements;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransforms;
            [ReadOnly] public LevelSettingComponent levelSettings;
            public DynamicBuffer<PullJointBuffer> jointBuffer;

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                var maxDistance = localTransforms[elements[0]].Scale / 1.5f;

                StaticMethod.SetJoint(ecb, jointBuffer, entity, elements[0], maxDistance,
                    levelSettings.indexConnection);

                for (var i = 0; i < elements.Length - 1; i++)
                {
                    StaticMethod.SetJoint(ecb, jointBuffer, elements[i], elements[i + 1], maxDistance,
                        levelSettings.indexConnection, "Joint " + i);
                }

                StaticMethod.SetJoint(ecb, jointBuffer, elements[^1], connectSphere.target, maxDistance,
                    levelSettings.indexConnection);

                ecb.RemoveComponent<ConnectSphere>(entity);
            }
        }

        private partial struct IncrementIndexConnectionJob : IJobEntity
        {
            private void Execute(ref LevelSettingComponent levelSettings)
            {
                levelSettings.indexConnection++;
            }
        }

        public void OnStopRunning(ref SystemState state)
        {
        }
    }
}