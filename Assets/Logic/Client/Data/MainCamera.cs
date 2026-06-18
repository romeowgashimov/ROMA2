using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Data
{
    public class MainCamera : IComponentData
    {
        public Camera Value;
    }
    public struct MainCameraTag : IComponentData { }

    public class PortraitCamera : IComponentData
    {
        public Camera Value;
    }
    
    public struct PortraitCameraTag : IComponentData { }
}