using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGame
{
    //シーンのタイプ
    public enum SceneType
    {
        Title,
        Preparation,
        MainGame,
        GameOver,
        Result,
    }

    /// <summary>
    /// シーンを遷移を管理するマネージャ―クラス
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        // シングルトンのインスタンス 
        public static SceneTransitionManager Instance { get; private set; }

        //ロードの仕様が定かではないためとりあえずシーン移行する際はフェイドさせることとする

        [Header("フェイドの設定")]
        [SerializeField] private CanvasGroup m_fadeCanvasGroup; // ロード画面やフェイドを表示するCanvasGroup
        [SerializeField] private float m_fadeOutTime = 0.35f;   // フェイドアウトにかかる時間
        [SerializeField] private float m_fadeInTime = 0.35f;    // フェイドインにかかる時間

        private bool m_isTransitioning = false;
        public bool IsTransitioning
        {
            get { return m_isTransitioning; }
        }

        private void Awake()
        {
            // シングルトンのインスタンスを設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            //このスクリプトの付いている子オブジェクトのCanvasGroupを取得
            if (m_fadeCanvasGroup == null)
            {
                m_fadeCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            //初期状態は透明
            if (m_fadeCanvasGroup != null)
            {
                m_fadeCanvasGroup.alpha = 0f;               //透明
                m_fadeCanvasGroup.blocksRaycasts = false;   //入力を受け付けない
                m_fadeCanvasGroup.interactable = false;
            }
        }

        //シーンをロードさせるときに呼ぶ関数
        public void Load(SceneType nextScene)
        {
            if (m_isTransitioning) return;  //現在シーンを遷移中なら何もしない
            StartCoroutine(LoadRoutine(nextScene)); //シーン遷移のコルーチンを開始
        }

        // フェイド処理のコルーチン
        private IEnumerator LoadRoutine(SceneType nextScene)
        {
            m_isTransitioning = true;

            // 入力ロック（UI含む
            // フェード用Canvasが入力を受け取り、背面のUI操作を防ぐ
            if (m_fadeCanvasGroup != null) m_fadeCanvasGroup.blocksRaycasts = true;

            // フェイドアウト
            yield return Fade(1f, m_fadeOutTime);

            // シーンのロード
            SceneManager.LoadScene(nextScene.ToString());

            // 1フレーム待つ（ロード直後の初期化安定）
            yield return null;

            // フェイドイン
            yield return Fade(0f, m_fadeInTime);

            // 入力アンロック
            if (m_fadeCanvasGroup != null) m_fadeCanvasGroup.blocksRaycasts = false;

            // シーン遷移完了
            m_isTransitioning = false;
        }

        /// <summary>
        /// フェイド処理のコルーチン
        /// </summary>
        /// <param name="targetAlpha">目標のα値</param>
        /// <param name="duration">フェイドにかかる時間</param>
        /// <returns></returns>
        private IEnumerator Fade(float targetAlpha, float duration)
        {
            if (m_fadeCanvasGroup == null || duration <= 0f)
            {
                if (m_fadeCanvasGroup != null) m_fadeCanvasGroup.alpha = targetAlpha;
                yield break;
            }

            float start = m_fadeCanvasGroup.alpha;  // 現在のα値
            float t = 0f;   // 経過時間

            while (t < duration)    // durationまでループ
            {
                t += Time.unscaledDeltaTime; // ポーズしてもフェードしたいなら unscaled
                float k = Mathf.Clamp01(t / duration);  // 0から1の補間値を計算
                m_fadeCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, k);    // 線形補間でα値を更新
                yield return null;
            }

            // 最終的に目標のα値に設定
            m_fadeCanvasGroup.alpha = targetAlpha;
        }

    }

}
