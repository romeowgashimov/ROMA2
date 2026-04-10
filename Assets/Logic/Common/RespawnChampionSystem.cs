using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class RespawnChampionSystem : SystemBase
    {
        public Action<int> OnUpdateRespawnCountdown;
        public Action OnRespawn;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkTime>();
            RequireForUpdate<MobaPrefabs>();
        }

        protected override void OnStartRunning()
        {
            if (SystemAPI.HasSingleton<RespawnEntityTag>()) return;
            Entity respawnPrefab = SystemAPI.GetSingleton<MobaPrefabs>().RespawnEntity;
            EntityManager.Instantiate(respawnPrefab);
        }

        protected override void OnUpdate()
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            NetworkTick currentTick = networkTime.ServerTick;

            bool isServer = World.IsServer();

            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach (DynamicBuffer<RespawnBufferElement> respawnBuffer in SystemAPI
                         .Query<DynamicBuffer<RespawnBufferElement>>()
                         .WithAll<RespawnTickCount, Simulate>())
            {
                NativeList<int> respawnsToCleanup = new(Allocator.Temp);
                
                for (int i = 0; i < respawnBuffer.Length; i++)
                {
                    RespawnBufferElement curRespawn = respawnBuffer[i];

                    if (currentTick.Equals(curRespawn.RespawnTick) || currentTick.IsNewerThan(curRespawn.RespawnTick))
                    {
                        if (isServer)
                        {
                            int networkId = SystemAPI.GetComponent<NetworkId>(curRespawn.NetworkEntity).Value;
                            PlayerRespawnInfo playerSpawnInfo = SystemAPI.GetComponent<PlayerRespawnInfo>(curRespawn.NetworkEntity);

                            Entity championPrefab = playerSpawnInfo.Champion;
                            Entity newChampion = ecb.Instantiate(championPrefab);
                            
                            ecb.SetComponent(newChampion, new GhostOwner { NetworkId = networkId });
                            ecb.SetComponent(newChampion, new Team { Value = playerSpawnInfo.Team });
                            ecb.SetComponent(newChampion, LocalTransform.FromPosition(playerSpawnInfo.SpawnPosition));
                            ecb.SetComponent(newChampion, new MoveTargetPosition { Value = playerSpawnInfo.SpawnPosition });
                            ecb.AppendToBuffer(curRespawn.NetworkEntity, new LinkedEntityGroup { Value = newChampion });
                            ecb.SetComponent(newChampion, new NetworkEntityReference { Value = curRespawn.NetworkEntity });
                            
                            respawnsToCleanup.Add(i);
                        }
                        else
                        {
                            OnRespawn?.Invoke();
                        }
                    }
                    else if (!isServer)
                    {
                        if (SystemAPI.TryGetSingleton(out NetworkId networkId))
                        {
                            if (networkId.Value == curRespawn.NetworkId)
                            {
                                uint ticksToRespawn = curRespawn.RespawnTick.TickIndexForValidTick -
                                                      currentTick.TickIndexForValidTick;
                                int simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
                                int secondsToStart = (int)math.ceil((float)ticksToRespawn / simulationTickRate);
                                OnUpdateRespawnCountdown?.Invoke(secondsToStart);
                            }
                        }
                    }
                }
                
                foreach (int respawnIndex in respawnsToCleanup)
                    respawnBuffer.RemoveAt(respawnIndex);
            }
            
            ecb.Playback(EntityManager);
        }
    }
}