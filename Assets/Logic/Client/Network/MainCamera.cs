using Unity.Entities;
using UnityEngine;

namespace Assets.Logic.Client
{
    public class MainCamera : IComponentData
    {
        public Camera Value;
    }
    public struct MainCameraTag : IComponentData { }
}