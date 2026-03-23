using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private void Start()
    {
        if (targetHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetHealth = player.GetComponent<Health>();
            }
        }

        Bind();
        Refresh();
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void Bind()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnHealthChanged(Health health)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (targetHealth == null) return;

        if (hpSlider != null)
        {
            hpSlider.maxValue = targetHealth.MaxHp;
            hpSlider.value = targetHealth.CurrentHp;
        }

        if (hpText != null)
        {
            hpText.text = $"{targetHealth.CurrentHp} / {targetHealth.MaxHp}";
        }
    }
}