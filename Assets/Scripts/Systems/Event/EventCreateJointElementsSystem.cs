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
    [UpdateAfter(typeof(EventCreateElementsSystem))]
    public partial struct EventCreateJointElementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elementsNotConnected = SystemAPI.QueryBuilder().WithAll<IsElementNotConnected>().Build();
            var entityElementsNotConnected = elementsNotConnected.ToEntityArray(Allocator.TempJob);
            Debug.Log(entityElementsNotConnected.Length);
            if (entityElementsNotConnected.Length <= 0) return;

            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var maxDistanceRange = SystemAPI.GetComponent<LocalTransform>(entityElementsNotConnected[0]).Scale / 1.5f;
            state.Dependency = new CreateConnectElementsJob
            {
                ecb = ecb, 
                elements = entityElementsNotConnected, 
                maxDistanceRange = maxDistanceRange
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveComponentIsElementNotConnectedJob()
            {
                ecb = ecb
            }.Schedule(state.Dependency);
        }

        private partial struct CreateConnectElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public NativeArray<Entity> elements;
            public float maxDistanceRange;

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                StaticMethod.CreateJoint(ecb, entity, elements[0], maxDistanceRange);

                for (var i = 0; i < elements.Length - 1; i++)
                {
                    StaticMethod.CreateJoint(ecb, elements[i], elements[i + 1], maxDistanceRange);
                }

                StaticMethod.CreateJoint(ecb, elements[^1], connectSphere.target, maxDistanceRange);

                ecb.RemoveComponent<ConnectSphere>(entity);
            }
        }
        
        private partial struct RemoveComponentIsElementNotConnectedJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;

            private void Execute(Entity entity, in IsElementNotConnected isElementNotConnected)
            {
                ecb.RemoveComponent<IsElementNotConnected>(entity);
            }
        }
    }
}