using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct NpcTargetingSystem : ISystem
    {
        private CollisionFilter _collisionFilter;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _collisionFilter = new()
            {
                BelongsTo = 1 << 6, //TargetCast
                CollidesWith = 1 << 1 | 1 << 2 | 1 << 4 //Champions, minions, structures
            };
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new NpcTargetingJob
            {
                CollisionFilter = _collisionFilter,
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = SystemAPI.GetComponentLookup<Team>(isReadOnly: true)
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct NpcTargetingJob : IJobEntity
    {
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;

        [BurstCompile]
        private void Execute(Entity npcEntity, ref NpcTargetEntity targetEntity,
            in LocalTransform transform, in NpcTargetRadius targetRadius)
        {
            NativeList<DistanceHit> hits = new(Allocator.TempJob);

            if (CollisionWorld.OverlapSphere(transform.Position, targetRadius.Value, ref hits, CollisionFilter))
            {
                float closestDistance = float.MaxValue;
                Entity closestEntity = Entity.Null;

                foreach (DistanceHit hit in hits)
                {
                    if (!TeamLookup.TryGetComponent(hit.Entity, out Team team)) continue;
                    if (team.Value == TeamLookup[npcEntity].Value) continue;
                    if (hit.Distance < closestDistance)
                    {
                        closestDistance = hit.Distance;
                        closestEntity = hit.Entity;
                    }
                }
                
                targetEntity.Value = closestEntity;
            }
            else
                targetEntity.Value = Entity.Null;
            
            hits.Dispose();
        }
    }
}