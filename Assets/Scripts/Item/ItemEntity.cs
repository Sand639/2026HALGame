using UnityEngine;

namespace MyGame
{
    [RequireComponent(typeof(Collider))]
    public class ItemEntity : Entity
    {
        [SerializeField] private string itemId = "Item";
        [SerializeField] private int amount = 1;

        public string ItemId => itemId;
        public int Amount => amount;

        public bool CanPickup => gameObject.activeInHierarchy;

        public void Pickup()
        {
            // ここで演出・SE・UI通知など入れられる
            gameObject.SetActive(false);
        }

        void Reset()
        {
            // Raycastで当てたいのでColliderはTrigger推奨しない
            var col = GetComponent<Collider>();
            col.isTrigger = false;
        }
    }
}
