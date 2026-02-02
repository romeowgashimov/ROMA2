using Assets.Logic.Common;
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
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<TeamRequest, ReceiveRpcCommandRequest>();

            state.RequireForUpdate(state.GetEntityQuery(builder));
            state.RequireForUpdate<MobaPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            Entity champPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Champion;

            foreach((TeamRequest teamRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity) in SystemAPI
            .Query<TeamRequest, ReceiveRpcCommandRequest>()
            .WithEntityAccess())
            {
                ecb.DestroyEntity(requestEntity);
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                TeamType requestedTeamType = teamRequest.Value;

                if (requestedTeamType == TeamType.AutoAssign)
                    requestedTeamType = TeamType.Blue;

                int clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;

                float3 spawnPos;
                switch (requestedTeamType)
                {
                    case TeamType.Blue:
                        spawnPos = new(-50f, 1, -50f);
                        break;
                    
                    case TeamType.Red:
                        spawnPos = new(50f, 1, 50f);
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

                Debug.Log($"Server is assigning Client ID: {clientId} to the {requestedTeamType} team");
            }

            ecb.Playback(state.EntityManager);
        }
    }
}