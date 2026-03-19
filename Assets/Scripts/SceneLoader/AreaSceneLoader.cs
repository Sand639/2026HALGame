using UnityEngine;

namespace MyGame
{
    public class AreaSceneLoader : MonoBehaviour
    {
        [SerializeField] private SceneType nextScene = SceneType.MainGame;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool oneShot = true;

        private bool done;

        private void OnTriggerEnter(Collider other)
        {
            if (done) return;
            if (!other.CompareTag(playerTag)) return;

            if (SceneTransitionManager.Instance != null &&
                SceneTransitionManager.Instance.IsTransitioning) return;

            SceneTransitionManager.Instance.Load(nextScene);

            if (oneShot) done = true;
        }
    }
}
