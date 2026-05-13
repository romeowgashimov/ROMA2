using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerProcessGameEntryRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GameStartProperties>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EntryConnectionRequest, ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));
            state.RequireForUpdate<ChampionPrefabElement>();
            state.RequireForUpdate<MobaPrefabs>();
        }

        [BurstDiscard]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            Entity gamePropertiesEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
            GameStartProperties gameStartProperties = SystemAPI.GetComponent<GameStartProperties>(gamePropertiesEntity);
            TeamPlayerCounter teamPlayerCounter = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropertiesEntity);
            DynamicBuffer<SpawnOffset> spawnOffsets = SystemAPI.GetBuffer<SpawnOffset>(gamePropertiesEntity);
            
            foreach ((EntryConnectionRequest entryRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity) in
                     SystemAPI
                         .Query<EntryConnectionRequest, ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(requestEntity);
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                TeamType requestedTeamType = entryRequest.Team;

                if (requestedTeamType == TeamType.AutoAssign)
                {
                    if (teamPlayerCounter.BlueTeamPlayers > teamPlayerCounter.RedTeamPlayers)
                        requestedTeamType = TeamType.Red;
                    else if (teamPlayerCounter.RedTeamPlayers >= teamPlayerCounter.BlueTeamPlayers)
                        requestedTeamType = TeamType.Blue;
                }

                int clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;

                float3 spawnPos;
                switch (requestedTeamType)
                {
                    case TeamType.Blue:
                        if (teamPlayerCounter.BlueTeamPlayers >= gameStartProperties.MaxPlayersPerTeam)
                        {
                            Debug.Log($"Blue Team is full. Client ID: {clientId} is spectating the game");
                            continue;
                        }

                        spawnPos = new(-50f, 1, -50f);
                        spawnPos += spawnOffsets[teamPlayerCounter.BlueTeamPlayers].Value;
                        teamPlayerCounter.BlueTeamPlayers++;
                        break;

                    case TeamType.Red:
                        if (teamPlayerCounter.RedTeamPlayers >= gameStartProperties.MaxPlayersPerTeam)
                        {
                            Debug.Log($"Red Team is full. Client ID: {clientId} is spectating the game");
                            continue;
                        }

                        spawnPos = new(50f, 1, 50f);
                        spawnPos += spawnOffsets[teamPlayerCounter.RedTeamPlayers].Value;
                        teamPlayerCounter.RedTeamPlayers++;
                        break;

                    default:
                        continue;
                }
                
                Entity pending = ecb.CreateEntity();
                ecb.AddComponent(pending, new PendingSpawn
                {
                    RequestSourceConnection = requestSource.SourceConnection,
                    SpawnPos = spawnPos,
                    CharacterId = (int)entryRequest.ChampionId,
                    Team = requestedTeamType,
                    ClientId = clientId
                });
                ecb.SetName(pending, $"Pending");
                
                foreach ((RefRO<NetworkId> _, Entity connectionEntity) in SystemAPI
                             .Query<RefRO<NetworkId>>()
                             .WithEntityAccess())
                {
                    Entity rpcRequest = ecb.CreateEntity();
                    ecb.AddComponent(rpcRequest, new LoadCharacterRequest { CharacterId = entryRequest.ChampionId });
                    ecb.AddComponent(rpcRequest, new SendRpcCommandRequest { TargetConnection = connectionEntity });
                }
            }

            ecb.Playback(state.EntityManager);
            SystemAPI.SetSingleton(teamPlayerCounter);
        }
    }
}