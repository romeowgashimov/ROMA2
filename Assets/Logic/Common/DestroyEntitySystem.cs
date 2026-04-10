using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RespawnEntityTag>();
            state.RequireForUpdate<MobaPrefabs>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            NetworkTick currentTick = networkTime.ServerTick;
            
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((RefRW<LocalTransform> transform, Entity entity) in SystemAPI
                         .Query<RefRW<LocalTransform>>()
                         .WithAll<DestroyEntityTag, Simulate>()
                         .WithEntityAccess())
            {
                if(state.World.IsServer())
                {
                    if (SystemAPI.HasComponent<GameOverOnDestroyTag>(entity))
                    {
                        Entity gameOverPrefab = SystemAPI.GetSingleton<MobaPrefabs>().GameOverEntity;
                        Entity gameOverEntity = ecb.Instantiate(gameOverPrefab);

                        TeamType losing = SystemAPI.GetComponent<Team>(entity).Value;
                        TeamType winning = losing == TeamType.Blue ? TeamType.Red : TeamType.Blue;
                        //Debug.Log($"{winning.ToString()} Team Won!");

                        ecb.SetComponent(gameOverEntity, new WinningTeam { Value = winning });
                    }

                    if (SystemAPI.HasComponent<ChampTag>(entity))
                    {
                        Entity networkEntity = SystemAPI.GetComponent<NetworkEntityReference>(entity).Value;
                        Entity respawnEntity = SystemAPI.GetSingletonEntity<RespawnEntityTag>();
                        uint respawnTickCount = SystemAPI.GetComponent<RespawnTickCount>(respawnEntity).Value;

                        NetworkTick respawnTick = currentTick;
                        respawnTick.Add(respawnTickCount);
                        
                        ecb.AppendToBuffer(respawnEntity, new RespawnBufferElement
                        {
                            NetworkEntity = networkEntity,
                            RespawnTick = respawnTick,
                            NetworkId = SystemAPI.GetComponent<NetworkId>(networkEntity).Value
                        });
                    }
                    
                    ecb.DestroyEntity(entity);
                }
                else
                    transform.ValueRW.Position = new(1000f, 0, 1000f);
            }
        }
    }
}