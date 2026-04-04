using System;
using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientStartGameSystem : SystemBase
    {
        public Action<int> OnUpdatePlayersRemainingToStart;
        public Action OnStartGameCountdown;
        
        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach ((PlayersRemainingToStart playersRemainingToStart, Entity entity) in SystemAPI
                         .Query<PlayersRemainingToStart>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                OnUpdatePlayersRemainingToStart?.Invoke(playersRemainingToStart.Value);
            }

            foreach ((GameStartTickRpc gameStartTick, Entity entity) in SystemAPI
                         .Query<GameStartTickRpc>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                OnStartGameCountdown?.Invoke();
                
                Entity gameStartEntity = ecb.CreateEntity();
                ecb.AddComponent(gameStartEntity, new GameStartTick
                {
                    Value = gameStartTick.Value
                });
            }
            
            ecb.Playback(EntityManager);
        }
    }
}