using Logic.Server;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using Unity.Transforms;

namespace Logic.Common
{
    public partial struct ServerAuthorizeCharactersSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GameStartProperties>();
            state.RequireForUpdate<ChampionPrefabElement>();
            state.RequireForUpdate<MobaPrefabs>();

            /*EntityQuery _query = SystemAPI.QueryBuilder()
                .WithAll<PendingSpawn, LoadedEntity>()
                .Build();
            
            state.RequireForUpdate(_query);*/
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            Entity gamePropertiesEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
            GameStartProperties gameStartProperties = SystemAPI.GetComponent<GameStartProperties>(gamePropertiesEntity);
            TeamPlayerCounter teamPlayerCounter = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropertiesEntity);
            
            foreach ((PendingSpawn pending, LoadedEntity loadRequestEntity, Entity pendingEntity) in SystemAPI
                         .Query<PendingSpawn, LoadedEntity>()
                         .WithEntityAccess())
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, loadRequestEntity.Value))
                    continue;
                
                Entity champPrefab = SystemAPI.GetComponent<PrefabRoot>(loadRequestEntity.Value).Root;
                bool isLoaded = champPrefab != Entity.Null;
                if (!isLoaded) continue;

                Entity newChamp = ecb.Instantiate(champPrefab);
                ecb.SetName(newChamp, "Champion");
                LocalTransform localTransform = LocalTransform.FromPosition(pending.SpawnPos);
                ecb.SetComponent(newChamp, localTransform);

                ecb.SetComponent(newChamp, new Team { Value = pending.Team });
                ecb.SetComponent(newChamp, new GhostOwner { NetworkId = pending.ClientId });

                ecb.AppendToBuffer(pending.RequestSourceConnection, new LinkedEntityGroup { Value = newChamp });

                ecb.SetComponent(newChamp, new NetworkEntityReference { Value = pending.RequestSourceConnection });
                ecb.AddComponent(pending.RequestSourceConnection, new PlayerRespawnInfo
                {
                    Champion = champPrefab,
                    Team = pending.Team,
                    SpawnPosition = pending.SpawnPos
                });
                
                int playersRemainingToStart =
                    gameStartProperties.MinPlayersToStartGame - teamPlayerCounter.TotalPlayers;

                Entity gameStartRpc = ecb.CreateEntity();
                if (playersRemainingToStart <= 0 && !SystemAPI.HasSingleton<GameplayingTag>())
                {
                    int simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
                    uint ticksUntilStart = (uint)(simulationTickRate * gameStartProperties.CountdownTime);
                    NetworkTick gameStartTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
                    gameStartTick.Add(ticksUntilStart);
                    
                    ecb.AddComponent(gameStartRpc, new GameStartTickRpc
                    {
                        Value = gameStartTick
                    });
                    
                    Entity gameStartEntity = ecb.CreateEntity();
                    ecb.AddComponent(gameStartEntity, new GameStartTick
                    {
                        Value = gameStartTick
                    });
                }
                else
                {
                    ecb.AddComponent(gameStartRpc, new PlayersRemainingToStart
                    {
                        Value = playersRemainingToStart
                    });
                }
                ecb.AddComponent<SendRpcCommandRequest>(gameStartRpc);
                
                ecb.DestroyEntity(pendingEntity);
            }

            ecb.Playback(state.EntityManager);
            SystemAPI.SetSingleton(teamPlayerCounter);
        }
    }
}