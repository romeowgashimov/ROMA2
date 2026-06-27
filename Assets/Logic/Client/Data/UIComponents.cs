using ROMA2.Logic.Client.Controllers;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ROMA2.Logic.Client.Data
{
    public class HealthBarUIReference : ICleanupComponentData
    {
        public GameObject Value;
    }

    public struct HealthBarOffset : IComponentData
    {
        public float3 Value;
    }

    public struct UpdatedHP4UI : IComponentData
    {
        public float Current;
        public float Max;
    }
    
    public struct UpdatedMana4UI : IComponentData
    {
        public float Current;
        public float Max;
    }

    public struct UpdatedChars : IComponentData
    {
        public int PhysicalPower;
        public int MagicalPower;
        public int PhysicalDefense;
        public int MagicalDefense;
        public float AttackSpeed;
        public int MoveSpeed;
    }
    
    public struct CharsChangeMask
    {
        public bool PhysicalPowerChanged;
        public bool MagicalPowerChanged;
        public bool PhysicalDefenseChanged;
        public bool MagicalDefenseChanged;
        public bool AttackSpeedChanged;
        public bool MoveSpeedChanged;
        
        // Были ли вообще изменения
        public bool HasChanges => PhysicalPowerChanged || MagicalPowerChanged || 
            PhysicalDefenseChanged || MagicalDefenseChanged || 
            AttackSpeedChanged || MoveSpeedChanged;
    }

    // Нужен для отобржанеия урона
    public struct CachedDamageElement : IBufferElementData
    {
        public Entity Receiver;
        public int PhysicalDamage;
        public int MagicalDamage;
        public int TrueDamage;
        public int AbilityIndex;
    }

    public struct CachedDamage : IComponentData
    {
        public int PhysicalDamage;
        public int MagicalDamage;
        public int TrueDamage;
        public float LifeTime;
    }
    
    public class SkillShotUIReference : ICleanupComponentData
    {
        public GameObject Value;
    }

    public class UIPrefabs : IComponentData
    {
        public GameObject HealthBar;
        public GameObject Visualizer;
        public GameObject SkillShot;
    }

    public class DamageVisualizerUIReference : ICleanupComponentData
    {
        public GameObject Value;
    }

    public class ModelPrefabs : IComponentData
    {
        public GameObject Minion;
        public GameObject Champion1;
        public GameObject Champion2;

        public GameObject Get(int index)
        {
            return index switch
            {
                0 => Minion,
                1 => Champion1,
                2 => Champion2,
                _ => null,
            };
        }
    }

    public struct ModelId : IComponentData
    {
        public int Value;
    }

    public class ModelReference : ICleanupComponentData
    {
        public GameObject Value;
    }
}