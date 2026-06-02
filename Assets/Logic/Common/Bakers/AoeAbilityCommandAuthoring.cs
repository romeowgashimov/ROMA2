using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Common.Extensions;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class AoeAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject AoeAbilityPrefab;
        
        private class SkillShotAbilityBaker : Baker<AoeAbilityCommandAuthoring>
        {
            public override void Bake(AoeAbilityCommandAuthoring commandAuthoring)
            {
                this.BakeAbilityCommand<AoeAbilityCommandAuthoring, 
                    AoeAbilityCommand>(commandAuthoring.AoeAbilityPrefab);
            }
        }
    }
}