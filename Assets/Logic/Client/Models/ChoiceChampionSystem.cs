using System;
using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Client.Models
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ChoiceChampionSystem : SystemBase
    {
        public Action OnReadyToChoice;

        protected override void OnCreate()
        {
            RequireForUpdate<ChoiceChampionRpc>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach ((ChoiceChampionRpc _, Entity rpc)  in SystemAPI
                         .Query<ChoiceChampionRpc>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(rpc);
                
                OnReadyToChoice?.Invoke();
            }
            
            ecb.Playback(EntityManager);
        }
    }
}