using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct MaxHealthPoints : IComponentData
    {
        [GhostField] public int Value;
    }
        
    public struct CurrentHealthPoints : IComponentData
    {
        [GhostField] public int Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct DamageBufferElement : IBufferElementData
    {
        public int Value;
    }
        
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct DamageThisTick : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public int Value;
    }

    public struct AbilityPrefabs : IComponentData
    {
        public Entity Ability1;
        public Entity Ability2;
        public Entity Ability3;
        public Entity Ability4;
        
        public int Length => 4;
        
        public Entity this[int index] => index switch
        {
            0 => Ability1,
            1 => Ability2,
            2 => Ability3,
            3 => Ability4,
            _ => default
        };
    }

    public struct DestroyOnTimer : IComponentData
    {
        public float Value;
    }
    
    public struct DestroyAtTick : IComponentData
    {
        [GhostField] public NetworkTick Value;
    }
    
    public struct DestroyEntityTag : IComponentData { }

    public struct DamageOnTrigger : IComponentData
    {
        public int Value;
    }

    public struct AlreadyDamagedEntity : IBufferElementData
    {
        public Entity Value;
    }

    public struct AbilityCooldownTicks : IComponentData
    {
        public uint AoeAbility;
        public uint SkillShotAbility;
        public int Length => 2;

        public uint this[int index] => index switch
        {
            0 => AoeAbility,
            1 => SkillShotAbility,
            _ => uint.MaxValue
        };
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityCooldownTargetTicks : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick AoeAbility;
        public NetworkTick SkillShotAbility;

        public int Length => 2;
        
        public NetworkTick this[int index]
        {
            get
            {
                return index switch
                {
                    0 => AoeAbility,
                    1 => SkillShotAbility,
                    _ => NetworkTick.Invalid
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        AoeAbility = value;
                        break;
                    case 1:
                        SkillShotAbility = value;
                        break;
                }
            }
        }
    }

    public struct AimSkillShotTag : IComponentData { }

    public struct AbilityMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct NpcAttackRadius : IComponentData
    {
        public float Value;
    }
    
    public struct NpcDetectionRadius : IComponentData
    {
        public float Value;
    }

    public struct NpcTargetEntity : IComponentData
    {
        [GhostField] public Entity Value;
    }
    
    public struct AggressionTag : IComponentData, IEnableableComponent { }

    public struct LastTargetPosition : IComponentData
    {
        public float3 Value;
    }
    
    public struct InAttackArea : IComponentData, IEnableableComponent { }
    
    public struct NpcAttackProperties : IComponentData
    {
        public float3 FirePointOffset;
        public uint CooldownTickCount;
        public Entity AttackPrefab;
    }

    public struct NpcAttackCooldown : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick Value;
    }

    public struct GameOverOnDestroyTag : IComponentData { }

    public struct DefaultAttackTarget : IComponentData
    {
        public Entity Value;
    }
}