using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using static Unity.Entities.SystemAPI;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(DestroyEntitySystem))]
    [BurstCompile]
    public partial struct RegisterNpcContainer : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcsContainer>();
            state.RequireForUpdate<NewNpcTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity npcContainer = GetSingletonEntity<NpcsContainer>();
            DynamicBuffer<RedNpcBufferElement> redNpcs = GetBuffer<RedNpcBufferElement>(npcContainer);
            DynamicBuffer<BlueNpcBufferElement> blueNpcs = GetBuffer<BlueNpcBufferElement>(npcContainer);

            state.Dependency = new RegisterNpcContainerJob
            {
                RedNpcs = redNpcs,
                BlueNpcs = blueNpcs
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new UnRegisterNpcContainerJob
            {
                RedNpcs = redNpcs,
                BlueNpcs = blueNpcs
            }.ScheduleParallel(state.Dependency);

        }
    }
    
    [BurstCompile]
    public partial struct RegisterNpcContainerJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<RedNpcBufferElement> RedNpcs;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<BlueNpcBufferElement> BlueNpcs;

        public void Execute(Team team, EnabledRefRW<NewNpcTag> newNpcTag, Entity npc)
        {
            switch (team.Value)
            {
                case TeamType.Red:
                {
                    RedNpcBufferElement newNpc =  new() { Value = npc };
                    RedNpcs.Add(newNpc);
                    break;
                }
                case TeamType.Blue:
                {
                    BlueNpcBufferElement newNpc =  new() { Value = npc };
                    BlueNpcs.Add(newNpc);
                    break;
                }
            }

            newNpcTag.ValueRW = false;
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(DestroyEntityTag), typeof(NpcTargetEntity))]
    public partial struct UnRegisterNpcContainerJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<RedNpcBufferElement> RedNpcs;
        [NativeDisableParallelForRestriction]
        public DynamicBuffer<BlueNpcBufferElement> BlueNpcs;

        public void Execute(Entity npc, in Team team)
        {
            switch (team.Value)
            {
                case TeamType.Red:
                {
                    for (int i = 0; i < RedNpcs.Length; i++)
                    {
                        if (RedNpcs[i].Value != npc) continue;
                        RedNpcs.RemoveAtSwapBack(i);
                        break;
                    }

                    break;
                }
                case TeamType.Blue:
                {
                    for (int i = 0; i < BlueNpcs.Length; i++)
                    {
                        if (BlueNpcs[i].Value != npc) continue;
                        BlueNpcs.RemoveAtSwapBack(i);
                        break;
                    }

                    break;
                }
            }
        }
    }
}