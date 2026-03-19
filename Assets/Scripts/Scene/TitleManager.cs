using MyGame;
using UnityEngine;
using UnityEngine.InputSystem;


namespace MyGame
{
    /// <summary>
    /// タイトル画面の管理を行うクラス
    /// </summary>
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private SceneType nextScene = SceneType.MainGame;

        // UI Button の OnClick から呼ぶ用
        public void OnClickStart()
        {
            SceneTransitionManager.Instance.Load(nextScene);
        }

        // キーでも開始したい場合（Enter / Space）
        private void Update()
        {
            if (SceneTransitionManager.Instance != null &&
                SceneTransitionManager.Instance.IsTransitioning) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                SceneTransitionManager.Instance.Load(nextScene);
            }

        }

    }

}


