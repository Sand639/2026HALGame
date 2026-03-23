using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);

    private Transform targetRoot;

    private void Start()
    {
        if (targetHealth == null)
        {
            targetHealth = GetComponentInParent<Health>();
        }

        if (targetHealth != null)
        {
            targetRoot = targetHealth.transform;
            targetHealth.OnHealthChanged += OnHealthChanged;
        }

        Refresh();
    }

    private void LateUpdate()
    {
        if (targetRoot != null)
        {
            transform.position = targetRoot.position + worldOffset;
        }
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(Health health)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (targetHealth == null || hpSlider == null) return;

        hpSlider.maxValue = targetHealth.MaxHp;
        hpSlider.value = targetHealth.CurrentHp;
    }
}