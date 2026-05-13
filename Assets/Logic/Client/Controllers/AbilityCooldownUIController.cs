using UnityEngine;
using UnityEngine.UI;

namespace Logic.Client
{
    public class AbilityCooldownUIController : MonoBehaviour
    {
        public static AbilityCooldownUIController Instance;
        
        [SerializeField] private Image _aoeAbilityMask;
        [SerializeField] private Image _skillShotAbilityMask;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _aoeAbilityMask.fillAmount = 0f;
            _skillShotAbilityMask.fillAmount = 0f;
        }

        public void UpdateMask(int index, float fillAmount)
        {
            switch (index)
            {
                case 0:
                    _aoeAbilityMask.fillAmount = fillAmount;
                    break;
                case 1:
                    _skillShotAbilityMask.fillAmount = fillAmount;
                    break;
            }
        }
    }
}