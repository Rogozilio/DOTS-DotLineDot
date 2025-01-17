using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(EventCreateAndConnectElementsSystem))]
    [UpdateAfter(typeof(InitNotActiveSphereSystem))]
    public partial struct SpawnSphereSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<NotActiveSphereBuffer>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSinglton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSinglton.CreateCommandBuffer(state.WorldUnmanaged);
            var sphereBuffer = SystemAPI.GetSingletonBuffer<NotActiveSphereBuffer>();
            
            state.Dependency = new SpawnSphereJob
            {
                ecb = ecb,
                sphereBuffer = sphereBuffer,
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>()
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct SpawnSphereJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public DynamicBuffer<NotActiveSphereBuffer> sphereBuffer;
            [ReadOnly]public LevelSettingComponent levelSetting;
            public void Execute(Entity entity,in SpawnSphereComponent spawnSphereComponent, in LocalTransform transform)
            {
                if(sphereBuffer.Length == 0) return;
                ecb.SetComponent(sphereBuffer[^1].value, transform);

                if (spawnSphereComponent.isAddConnectSphere)
                {
                    ecb.SetComponentEnabled<IsMouseMove>(sphereBuffer[^1].value, true);
                    ecb.AddComponent(entity, new ConnectSphere(){target = sphereBuffer[^1].value});
                    var elementBuffer = new IndexConnectionBuffer() { value = levelSetting.indexConnection };
                    ecb.AppendToBuffer(entity, elementBuffer);
                    ecb.AppendToBuffer(sphereBuffer[^1].value, elementBuffer);
                }
                
                sphereBuffer.RemoveAt(sphereBuffer.Length - 1);
                ecb.RemoveComponent<SpawnSphereComponent>(entity);
            }
        }
    }
}