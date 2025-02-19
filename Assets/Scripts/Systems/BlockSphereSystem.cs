using Components;
using Components.Shared;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BlockSphereSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<IndexSharedComponent> indexes,
                Allocator.TempJob);

            var listCountElement = new NativeArray<int>(indexes.Length, Allocator.TempJob);
            
            var queryElements = SystemAPI.QueryBuilder()
                .WithPresent<TargetGravityComponent, IndexSharedComponent>()
                .Build();
           
            foreach (var index in indexes)
            {
                if (index.value == -1) continue;

                queryElements.SetSharedComponentFilter(index);

                listCountElement[index.value] = queryElements.CalculateEntityCount();
            }

            state.Dependency = new BlockSphereJob
            {
                listCountElements = listCountElement
            }.Schedule(state.Dependency);
            state.Dependency = new UnblockSphereJob
            {
                listCountElements = listCountElement
            }.Schedule(state.Dependency);
            state.Dependency = listCountElement.Dispose(state.Dependency);
            state.Dependency = indexes.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(IsBlockedSphere))]
        public partial struct BlockSphereJob : IJobEntity
        {
            public NativeArray<int> listCountElements;

            public void Execute(EnabledRefRW<IsBlockedSphere> isBlockedSphere, in SphereComponent sphere,
                in IndexSharedComponent index)
            {
                if(index.value < 0) return;
                
                isBlockedSphere.ValueRW = listCountElements[index.value] >= sphere.countElements;
            }
        }
        
        [BurstCompile]
        public partial struct UnblockSphereJob : IJobEntity
        {
            public NativeArray<int> listCountElements;

            public void Execute(EnabledRefRW<IsBlockedSphere> isBlockedSphere, in SphereComponent sphere,
                in IndexSharedComponent index)
            {
                if(index.value < 0) return;
                
                isBlockedSphere.ValueRW = listCountElements[index.value] >= sphere.countElements - 1;
            }
        }
    }
}