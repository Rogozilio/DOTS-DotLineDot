using Components;
using Components.Shared;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    public partial struct ResizeSphereSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<IndexSharedComponent> indexes,
                Allocator.TempJob);

            var querySphere = SystemAPI.QueryBuilder()
                .WithAll<IndexSharedComponent, SphereComponent>()
                .Build();
            var queryElements = SystemAPI.QueryBuilder()
                .WithAll<IndexSharedComponent>()
                .WithDisabled<TargetGravityComponent>()
                .Build();

            foreach (var index in indexes)
            {
                querySphere.SetSharedComponentFilter(index);
                queryElements.SetSharedComponentFilter(index);

                var spheres = querySphere.ToComponentDataArray<SphereComponent>(Allocator.Temp);

                if (spheres.Length == 0) continue;

                float limit = spheres[0].countElements;

                float count = limit - queryElements.CalculateEntityCount();

                if (count == 0) continue;

                //Debug.Log(index.value + " " + count + " " + limit);

                state.Dependency = new ResizeSphereJob
                {
                    index = index.value,
                    size = math.max(0.5f, 0.5f + 0.5f * (count / limit))
                }.Schedule(state.Dependency);

                spheres.Dispose();
            }

            indexes.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(SphereComponent))]
        public partial struct ResizeSphereJob : IJobEntity
        {
            [ReadOnly] public int index;
            [ReadOnly] public float size;

            public void Execute(in IndexSharedComponent index, ref LocalTransform transform)
            {
                if (this.index != index.value) return;

                transform.Scale = size;
            }
        }
    }
}