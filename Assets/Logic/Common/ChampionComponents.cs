using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct ChampTag : IComponentData { }
    
    public struct NewChampTag : IComponentData { }
    
    public struct OwnerChampTag : IComponentData { }

    public struct Owner : IComponentData
    {
        public Entity Value;
    } 
    
    public struct ChampionPrefabElement : IBufferElementData
    {
        public int Id;
        public EntityPrefabReference Value;
    }
    
    public struct Team : IComponentData
    {
        [GhostField] public TeamType Value;
    }
    
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }
    
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct InputMoveTargetPosition : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
        public bool Flag;
    }

    public struct MoveTargetPosition : IComponentData
    {
        /*Убрал синхронизацию с сервером, pathfinding у клиента срабатывал раньше,
        чем синхронизация, поэтому находился путь к не той точке */
        public float3 Value;
        public bool Flag;
    } 
    
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityInput : IInputComponentData
    {
        [GhostField] public InputEvent Ability1;
        [GhostField] public InputEvent Ability2;
        [GhostField] public InputEvent Ability3;
        [GhostField] public InputEvent Ability4;
        [GhostField] public InputEvent ConfirmAbility;
        [GhostField] public InputEvent CancelAbility;
        public bool NeedToConfirmAbilities;
        public int Length => 4;
        
        public InputEvent this[int index] => index switch
        {
            0 => Ability1,
            1 => Ability2,
            2 => Ability3,
            3 => Ability4,
            _ => ConfirmAbility
        };
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AimInput : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }
}