using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Client.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Entities.World;

namespace ROMA2.Logic.Client.Controllers
{
    public class BarsUIController : MonoBehaviour
    {
        [SerializeField] public Slider HealthBarSlider;
        [SerializeField] public Slider ManaBarSlider;
        [SerializeField] public TextMeshProUGUI HealthBarText;
        [SerializeField] public TextMeshProUGUI ManaBarText;
        [SerializeField] public TextMeshProUGUI PhysicalPowerText;
        [SerializeField] public TextMeshProUGUI MagicalPowerText;
        [SerializeField] public TextMeshProUGUI PhysicalDefText;
        [SerializeField] public TextMeshProUGUI MagicalDefText;
        [SerializeField] public TextMeshProUGUI AttackSpeedText;
        [SerializeField] public TextMeshProUGUI MoveSpeedText;

        private void OnEnable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;

            ChangeCharsEventSystem eventSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ChangeCharsEventSystem>();
            if (eventSystem == null) return;
            
            eventSystem.OnHealthChanged += StoreHealthTarget;
            eventSystem.OnManaChanged += StoreManaTarget;
            eventSystem.OnCharsChanged += OnCharacterStatsChanged;
        }

        private void OnDisable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;

            ChangeCharsEventSystem eventSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ChangeCharsEventSystem>();
            if (eventSystem == null) return;

            eventSystem.OnHealthChanged -= StoreHealthTarget;
            eventSystem.OnManaChanged -= StoreManaTarget;
            eventSystem.OnCharsChanged -= OnCharacterStatsChanged;
        }

        private void StoreHealthTarget(float curPoints, int maxPoints)
        {
            if (HealthBarSlider == null) return;

            HealthBarSlider.maxValue = maxPoints;
            HealthBarSlider.value = curPoints;
            HealthBarText.text = $"{(int)curPoints}/{maxPoints}";
        }
        
        private void StoreManaTarget(float curPoints, int maxPoints)
        {
            if (ManaBarSlider == null) return;

            ManaBarSlider.maxValue = maxPoints;
            ManaBarSlider.value = curPoints;
            ManaBarText.text = $"{(int)curPoints}/{maxPoints}";
        }

        // Обработка пачки характеристик за один вызов
        private void OnCharacterStatsChanged(UpdatedChars allStats, CharsChangeMask mask)
        {
            if (mask.PhysicalPowerChanged && PhysicalPowerText != null)
                PhysicalPowerText.text = allStats.PhysicalPower.ToString();

            if (mask.MagicalPowerChanged && MagicalPowerText != null)
                MagicalPowerText.text = allStats.MagicalPower.ToString();

            if (mask.PhysicalDefenseChanged && PhysicalDefText != null)
                PhysicalDefText.text = allStats.PhysicalDefense.ToString();

            if (mask.MagicalDefenseChanged && MagicalDefText != null)
                MagicalDefText.text = allStats.MagicalDefense.ToString();

            if (mask.MoveSpeedChanged && MoveSpeedText != null)
                MoveSpeedText.text = allStats.MoveSpeed.ToString();

            if (mask.AttackSpeedChanged && AttackSpeedText != null)
                AttackSpeedText.text = allStats.AttackSpeed.ToString("F2");
        }
    }
}
