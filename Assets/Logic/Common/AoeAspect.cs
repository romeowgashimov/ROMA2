using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic.Common
{
    public readonly partial struct AoeAspect : IAspect
    {
        private readonly RefRO<AbilityInput> _inputAction;
        private readonly RefRO<AbilityPrefabs> _prefab;
        private readonly RefRO<Team> _team;
        private readonly RefRO<LocalTransform> _localTransform;
        
        public bool ShouldAttack => _inputAction.ValueRO.Value.IsSet;
        public Entity AbilityPrefab => _prefab.ValueRO.AoeAbility;
        public Team Team => _team.ValueRO;
        public float3 AttackPosition => _localTransform.ValueRO.Position;
    }
}