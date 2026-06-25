using ROMA2.Logic.Data;
using TMPro;
using UnityEngine;

namespace ROMA2.Logic.Client.Controllers
{
    public class DamageVisualizer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _physicalDamageUI;
        [SerializeField] private TextMeshProUGUI _magicalDamageUI;
        [SerializeField] private TextMeshProUGUI _trueDamageUI;
        
        public void ChangeDamage(DamageType type, int value)
        {
            switch (type)
            {
                case DamageType.Physical:
                    _physicalDamageUI.text = $"{value}";
                    break;
                case DamageType.Magical:
                    _magicalDamageUI.text = $"{value}";
                    break;
                case DamageType.True:
                    _trueDamageUI.text = $"{value}";
                    break;
            }
        }
    }
}