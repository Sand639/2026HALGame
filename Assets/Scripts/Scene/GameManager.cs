using MyGame;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainGameManager : MonoBehaviour
{
    // いまはデバッグ用：Escでタイトルへ戻るああ
    private void Update()
    {
        if (SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsTransitioning) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            SceneTransitionManager.Instance.Load(SceneType.Title);
        }
    }

    // 将来：死亡したら呼ぶ
    public void TriggerGameOver()
    {
        // いまはシーン未作成なら、とりあえずタイトルに戻すでもOK
        SceneTransitionManager.Instance.Load(SceneType.Title);
    }

    // 将来：クリアしたら呼ぶ
    public void TriggerGameClear()
    {
        SceneTransitionManager.Instance.Load(SceneType.Title);
    }

}