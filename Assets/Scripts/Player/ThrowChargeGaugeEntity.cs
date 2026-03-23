using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    public class ThrowChargeGaugeEntity : Entity
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Image fillImage;

        public override void InitEntity()
        {
            SetVisible(false);
            SetRate(0.0f);
        }

        public void SetVisible(bool visible)
        {
            if (rootObject != null)
                rootObject.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        public void SetRate(float rate)
        {
            rate = Mathf.Clamp01(rate);

            if (fillImage != null)
                fillImage.fillAmount = rate;
        }
    }
}