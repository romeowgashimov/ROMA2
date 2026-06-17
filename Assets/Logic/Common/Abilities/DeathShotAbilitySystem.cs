using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct DeathShotAbilitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new DeathShotAbilityJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DeathShotAbilityJob : IJobEntity
    {
        public void Execute(
            in DeathShotAbility ability,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in CombineCharsComponent combineCharsComponent,
            in DefaultDamage damage,
            ref DynamicBuffer<SendDamageElement> sendDamages,
            in AbilityIndex abilityIndex,
            in Owner owner)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                int physicalDamage = damage.PhysicalDamage;
                physicalDamage += ability.PhysicalPercentage / 100 * combineCharsComponent.PhysicalPower;

                sendDamages.Add(new()
                {
                    PhysicalDamage = physicalDamage,
                    Receiver = triggerInfoBuffer[i].Value,
                    Owner = owner.Value,
                    AbilityIndex = abilityIndex.Value
                });
                triggerInfoBuffer.RemoveAtSwapBack(i);
            }
        }
    }
}