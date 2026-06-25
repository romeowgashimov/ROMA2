using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Data
{
    public struct AttackSpeed : IComponentData
    {
        public float Value;
    }

    public struct PhysicalPower : IComponentData
    {
        public int Value;
    }
    
    public struct MagicalPower : IComponentData
    {
        public int Value;
    }

    public struct PhysicalArmor : IComponentData
    {
        public int Value;
    }

    public struct MagicalArmor : IComponentData
    {
        public int Value;
    }

    public struct HealthRegeneration : IComponentData
    {
        public float Value;
    }
    
    public struct ManaRegeneration : IComponentData
    {
        public float Value;
    }
    
    public struct CurrentMana : IComponentData
    {
        [GhostField(Quantization = 0)] public float Value;
    }
    
    public struct MaxMana : IComponentData
    {
        [GhostField] public int Value;
    }

    public enum DamageType : byte
    {
        Physical,
        Magical,
        True,
        All,
        None
    }
    
    // Изменение исходящего урона от персонажа
    public struct OutgoingDamageChangerElement : IBufferElementData // Буфер, так как может быть множество эффектов 
    {
        public int Value; // Количество урона
        public bool IsPercentage; // Процент или номинальный урон
        public DamageType Type; // Тип урона
    }

    // Изменение входящего урона персонажа
    public struct IncomingDamageChangerElement : IBufferElementData  
    {
        public int Value; // Количество урона
        public bool IsPercentage; // Процент или номинальный урон
        public DamageType Type; // Тип урона
    }

    [InternalBufferCapacity(0)] // Данные всегда хранятся в куче
    public struct IncomingDamageElement : IBufferElementData
    {
        // Не атака или умение, а владелец сущности, нанёсшей урон
        public Entity Owner;
        public int PhysicalDamage;
        public int MagicalDamage;
        public int TrueDamage;
        // Индекс базовой атаки -1
        public int AbilityIndex;
    }

    public struct SendDamageElement : IBufferElementData
    {
        // Не атака или умение, а владелец сущности, нанёсшей урон
        public Entity Owner;
        public Entity Receiver;
        public int PhysicalDamage;
        public int MagicalDamage;
        public int TrueDamage;
        // Индекс базовой атаки -1
        public int AbilityIndex;
    }

    [InternalBufferCapacity(0)]
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct ProcessedDamageElement : IBufferElementData
    {
        [GhostField] public Entity Receiver;
        [GhostField] public int PhysicalDamage;
        [GhostField] public int MagicalDamage;
        [GhostField] public int TrueDamage;
        [GhostField] public int AbilityIndex;
    }
    
    public struct DefaultDamage : IComponentData
    {
        // Допускаем смешанный урон, поэтому такой костыль
        public int PhysicalDamage;
        public int MagicalDamage;
        public int TrueDamage;
    }

    public struct DeathShotAbility : IComponentData
    {
        public int PhysicalPercentage;
    }

    public struct DeathSphereAbility : IComponentData
    {
        public int MagicalPercentage;
    }

    // Чтобы не лазить в лукапы сразу передаём всю инфу о владельце
    public struct CombineCharsComponent : IComponentData
    {
        public int PhysicalPower;
        public int MagicalPower;
        public int PhysicalArmor;
        public int MagicalArmor;
        public int MaxHP;
        public int CurrentHP;
        // public ...
    }
    
    public struct RangedAttack : IComponentData { }

    public struct AbilityIndex : IComponentData
    {
        public int Value;
    }

    public struct Vampirism : IComponentData
    {
        public int Value;
    }
}