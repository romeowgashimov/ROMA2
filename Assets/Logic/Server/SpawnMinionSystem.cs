using Assets.Logic.Common;
using Logic.Common;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;


namespace Logic.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct SpawnMinionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MinionPathContainer>();
            state.RequireForUpdate<MobaPrefabs>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach ((RefRW<MinionSpawnTimers> timers, MinionSpawnProperties properties) in SystemAPI
                         .Query<RefRW<MinionSpawnTimers>, MinionSpawnProperties>())
            {
                timers.ValueRW.DecrementTimers(deltaTime);
                if (timers.ValueRW.ShouldSpawn)
                {
                    SpawnOnEachLine(ref state);
                    timers.ValueRW.PlusCountSpawnedInWave();
                    if (timers.ValueRW.IsWaveSpawned(properties.CountToSpawnInWave))
                    {
                        timers.ValueRW.ResetWaveTimer(properties.TimeBetweenWaves);
                        timers.ValueRW.ResetMinionTimer(properties.TimeBetweenMinions);
                        timers.ValueRW.ResetSpawnCounter();
                    }
                    else
                        timers.ValueRW.ResetMinionTimer(properties.TimeBetweenMinions);
                }
            }
        }

        private void SpawnOnEachLine(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            Entity minionPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Minion;
            MinionPathContainer pathContainers = SystemAPI.GetSingleton<MinionPathContainer>();
            
            for (int i = 0; i <= pathContainers.Length; i++)
            {
                DynamicBuffer<MinionPathPosition> lane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers[i]);
                SpawnOnLine(ecb, minionPrefab, lane);
            }
        }

        private void SpawnOnLine(EntityCommandBuffer ecb, Entity minionPrefab, DynamicBuffer<MinionPathPosition> currentLane)
        {
            Entity newBlueMinion = ecb.Instantiate(minionPrefab);
            for (int i = 0; i < currentLane.Length; i++)
            {
                ecb.AppendToBuffer(newBlueMinion, currentLane[i]);
            }
            
            LocalTransform blueSpawnTransform = LocalTransform.FromPosition(currentLane[0].Value);
            ecb.SetComponent(newBlueMinion, blueSpawnTransform);
            ecb.SetComponent(newBlueMinion, new Team { Value = TeamType.Blue });
            
            Entity newRedMinion = ecb.Instantiate(minionPrefab);
            for (int i = currentLane.Length - 1; i >= 0 ; i--)
            {
                ecb.AppendToBuffer(newRedMinion, currentLane[i]);
            } 
            
            LocalTransform redSpawnTransform = LocalTransform.FromPosition(currentLane[^1].Value);
            ecb.SetComponent(newRedMinion, redSpawnTransform);
            ecb.SetComponent(newRedMinion, new Team { Value = TeamType.Red });
        }
    }
}