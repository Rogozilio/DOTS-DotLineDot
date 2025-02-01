using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

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
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var sphereBuffer = SystemAPI.GetSingletonBuffer<NotActiveSphereBuffer>();
            
            state.Dependency = new SpawnSphereJob
            {
                ecb = ecb,
                sphereBuffer = sphereBuffer,
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>()
            }.Schedule(state.Dependency);

            state.Dependency = new BlockSpawnSphereJob
            {
                ecb = ecb
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        public partial struct SpawnSphereJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public DynamicBuffer<NotActiveSphereBuffer> sphereBuffer;
            [ReadOnly]public LevelSettingComponent levelSetting;
            public void Execute(Entity entity,ref SpawnSphereComponent spawnSphere, in LocalTransform transform)
            {
                if(sphereBuffer.Length == 0) return;
                ecb.SetComponent(sphereBuffer[^1].value, transform);
                ecb.SetSharedComponent(sphereBuffer[^1].value, new IndexSharedComponent(){value = spawnSphere.index});

                if (spawnSphere.isAddConnectSphere)
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

        [BurstCompile]
        [WithAll(typeof(IsBlockedSphere))]
        [WithAll(typeof(SpawnSphereComponent))]
        public partial struct BlockSpawnSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public void Execute(Entity entity)
            {
                ecb.RemoveComponent<SpawnSphereComponent>(entity);
            }
        }
    }
}