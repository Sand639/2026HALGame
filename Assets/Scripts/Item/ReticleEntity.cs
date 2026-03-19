using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    public class ReticleEntity : Entity
    {
        [SerializeField] private Image reticleImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;

        public override void InitEntity()
        {
            if (reticleImage != null)
                reticleImage.color = normalColor;
        }

        public void SetActive(bool canPickup)
        {
            if (reticleImage == null) return;
            reticleImage.color = canPickup ? activeColor : normalColor;
        }
    }
}
