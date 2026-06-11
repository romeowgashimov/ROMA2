using ROMA2.Logic.Common.Extensions;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class HealthPointsAuthoring : MonoBehaviour
    {
        public int MaxHealthPoints;
        public Vector3 HealthBarOffset;

        public class HealthPointsBaker : Baker<HealthPointsAuthoring>
        {
            public override void Bake(HealthPointsAuthoring authoring)
            {
                this.BakeHealth(authoring.MaxHealthPoints);
            }
        }
    }
}