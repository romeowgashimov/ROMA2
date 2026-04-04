using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
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
            .WithAll<TeamRequest, ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));
            state.RequireForUpdate<MobaPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            Entity champPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Champion;

            Entity gamePropertiesEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
            GameStartProperties gameStartProperties = SystemAPI.GetComponent<GameStartProperties>(gamePropertiesEntity);
            TeamPlayerCounter teamPlayerCounter = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropertiesEntity);
            DynamicBuffer<SpawnOffset> spawnOffsets = SystemAPI.GetBuffer<SpawnOffset>(gamePropertiesEntity);
            
            foreach((TeamRequest teamRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity) in SystemAPI
                        .Query<TeamRequest, ReceiveRpcCommandRequest>()
                        .WithEntityAccess())
            {
                ecb.DestroyEntity(requestEntity);
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                TeamType requestedTeamType = teamRequest.Value;

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

                Entity newChamp = ecb.Instantiate(champPrefab);
                ecb.SetName(newChamp, "Champion");
                LocalTransform localTransform = LocalTransform.FromPosition(spawnPos);
                ecb.SetComponent(newChamp, localTransform);

                ecb.SetComponent(newChamp, new Team { Value = requestedTeamType });
                ecb.SetComponent(newChamp, new GhostOwner { NetworkId = clientId });

                ecb.AppendToBuffer(requestSource.SourceConnection, new LinkedEntityGroup { Value = newChamp });

                //Debug.Log($"Server is assigning Client ID: {clientId} to the {requestedTeamType} team");

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
            }

            ecb.Playback(state.EntityManager);
            SystemAPI.SetSingleton(teamPlayerCounter);
        }
    }
}