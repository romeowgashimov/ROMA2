using Logic.Common;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ROMA2.Logic.Helpers.Bakers
{
    public class NPCBasicRangedAttackAuthoring : MonoBehaviour
    {
        public float AttackRadius;
        public float DetectionRadius;
        public Vector3 FirePointOffset;
        public float AttackSpeed;
        public GameObject AttackPrefab;

        public NetCodeConfig NetCodeConfig;
        public int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;

        public class BasicRangedAttackBaker : Baker<NPCBasicRangedAttackAuthoring>
        {
            public override void Bake(NPCBasicRangedAttackAuthoring authoring)
            {
                this.BakeBehaviour(
                    authoring.AttackRadius,
                    authoring.DetectionRadius,
                    authoring.FirePointOffset,
                    authoring.AttackSpeed,
                    false,
                    authoring.AttackPrefab);
            }
        }
    }
}