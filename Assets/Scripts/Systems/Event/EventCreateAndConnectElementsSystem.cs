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
        private ComponentLookup<LocalTransform> _localTransforms;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConnectSphere>();
            state.RequireForUpdate<MultiSphereComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _localTransforms = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var levelSettings = SystemAPI.GetSingleton<MultiSphereComponent>();
            var elementsNotConnected = SystemAPI.QueryBuilder().WithAll<IsElementNotConnected>().Build();
            var entityElementsNotConnected = elementsNotConnected.ToEntityArray(Allocator.TempJob);
            
            _localTransforms.Update(ref state);
            
            state.Dependency = new CreateElementsJob
            {
                ecb = ecb,
                levelSettings = levelSettings
            }.Schedule(state.Dependency);
         
            state.Dependency = new CreateJointElementsJob
            {
                ecb = ecb,
                elements = entityElementsNotConnected,
                localTransforms = _localTransforms
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
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
            public MultiSphereComponent levelSettings;
            
            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                for (byte i = 0; i < levelSettings.countElements; i++)
                {
                    var newElement = ecb.Instantiate(levelSettings.prefabElement);
                    ecb.SetName(newElement, "Element " + i);
                    ecb.SetComponent(newElement, new ElementComponent()
                    {
                        id = i
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

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                if (elements.Length == 0) return;

                var maxDistance = localTransforms[elements[0]].Scale / 1.5f;

                StaticMethod.CreateJoint(ecb, entity, elements[0], maxDistance);

                for (var i = 0; i < elements.Length - 1; i++)
                {
                    StaticMethod.CreateJoint(ecb, elements[i], elements[i + 1], maxDistance);
                }

                StaticMethod.CreateJoint(ecb, elements[^1], connectSphere.target, maxDistance);

                ecb.RemoveComponent<ConnectSphere>(entity);
                ecb.RemoveComponent<TagElementsCreated>(entity);
            }
        }

        private partial struct RemoveComponentIsElementNotConnectedJob : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, in IsElementNotConnected isElementNotConnected)
            {
                ecb.RemoveComponent<IsElementNotConnected>(sortKey, entity);
            }
        }
    }
}