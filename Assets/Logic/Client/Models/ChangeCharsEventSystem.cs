using System;
using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;
using static Unity.Mathematics.math;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ChangeCharsEventSystem : SystemBase
    {
        public event Action<float, int> OnHealthChanged;
        public event Action<float, int> OnManaChanged;
        
        // Единственное событие для характеристик: передает копию структуры статов и маску изменений
        public event Action<UpdatedChars, CharsChangeMask> OnCharsChanged;
        
        protected override void OnUpdate()
        {
            // Здоровье
            foreach ((CurrentHealthPoints currHP, MaxHealthPoints maxHP, RefRW<UpdatedHP4UI> updated) in SystemAPI
                         .Query<CurrentHealthPoints, MaxHealthPoints, RefRW<UpdatedHP4UI>>()
                         .WithChangeFilter<CurrentHealthPoints, MaxHealthPoints>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                if (abs(currHP.Value - updated.ValueRO.Current) < 0.01f 
                    && abs(maxHP.Value - updated.ValueRO.Max) < 1) continue;

                updated.ValueRW.Current = currHP.Value;
                updated.ValueRW.Max = maxHP.Value;
                
                OnHealthChanged?.Invoke(currHP.Value, maxHP.Value);
            }
            
            // Мана
            foreach ((CurrentMana currentMana, MaxMana maxMana, RefRW<UpdatedMana4UI> updated) in SystemAPI
                         .Query<CurrentMana, MaxMana, RefRW<UpdatedMana4UI>>()
                         .WithChangeFilter<CurrentMana, MaxMana>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                if (abs(currentMana.Value - updated.ValueRO.Current) < 0.01f 
                    && abs(maxMana.Value - updated.ValueRO.Max) < 1) continue;

                updated.ValueRW.Current = currentMana.Value;
                updated.ValueRW.Max = maxMana.Value;
                
                OnManaChanged?.Invoke(currentMana.Value, maxMana.Value);
            }
            
            // Характеристики
            foreach ((PhysicalPower physPow, PhysicalArmor physArm, MagicalPower magPow, MagicalArmor magArm,
                         AttackSpeed attackSpeed, MoveSpeed moveSpeed, RefRW<UpdatedChars> updatedChars) in SystemAPI
                         .Query<PhysicalPower, PhysicalArmor, MagicalPower, MagicalArmor, 
                             AttackSpeed, MoveSpeed, RefRW<UpdatedChars>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                CharsChangeMask mask = new();

                if (physPow.Value != updatedChars.ValueRO.PhysicalPower)
                {
                    updatedChars.ValueRW.PhysicalPower = physPow.Value;
                    mask.PhysicalPowerChanged = true;
                }

                if (magPow.Value != updatedChars.ValueRO.MagicalPower)
                {
                    updatedChars.ValueRW.MagicalPower = magPow.Value;
                    mask.MagicalPowerChanged = true;
                }

                if (physArm.Value != updatedChars.ValueRO.PhysicalDefense)
                {
                    updatedChars.ValueRW.PhysicalDefense = physArm.Value;
                    mask.PhysicalDefenseChanged = true;
                }

                if (magArm.Value != updatedChars.ValueRO.MagicalDefense)
                {
                    updatedChars.ValueRW.MagicalDefense = magArm.Value;
                    mask.MagicalDefenseChanged = true;
                }

                if (abs(attackSpeed.Value - updatedChars.ValueRO.AttackSpeed) > 0.01f)
                {
                    updatedChars.ValueRW.AttackSpeed = attackSpeed.Value;
                    mask.AttackSpeedChanged = true;
                }

                if (moveSpeed.Value != updatedChars.ValueRO.MoveSpeed)
                {
                    updatedChars.ValueRW.MoveSpeed = moveSpeed.Value;
                    mask.MoveSpeedChanged = true;
                }

                // Вызываем событие ОДИН раз, если хоть что-то изменилось
                if (mask.HasChanges) OnCharsChanged?.Invoke(updatedChars.ValueRO, mask);
            }
        }

        protected override void OnDestroy()
        {
            OnHealthChanged = null;
            OnManaChanged = null;
            OnCharsChanged = null;
        }
    }
}