using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OrZero
{
    /// <summary>
    /// タイトル画面（Title シーン。SPEC §1.2）。
    /// ベストスコア（ランキングの1位）を表示し、START ボタンか Enter キーで Game シーンへ進む。終了ボタンでアプリを閉じる。
    /// ロゴ・キャッチコピー・ルール説明はシーンに置いた文字をそのまま出す（このクラスでは扱わない）。
    /// 表示する文字の組み立ては static 関数に分けてあり、EditMode テストで確かめられる
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        // ===== 調整値・参照（Inspector で設定） =====
        [SerializeField] private GameBalanceData balanceData;   // ゲーム全体の調整値（ランキングに残す件数を読むのに使う）
        [SerializeField] private TMP_Text bestScoreText;        // ベストスコアの表示
        [SerializeField] private Button startButton;            // START ボタン
        [SerializeField] private Button quitButton;             // 終了ボタン（ビルドではアプリを閉じる。Editor では Console に出すだけ）

        /// <summary>
        /// 起動時に Inspector の設定漏れを確かめ、ボタンが押されたときの処理を登録する。設定漏れがあればこのコンポーネントを止める
        /// </summary>
        private void Awake()
        {
            if (balanceData == null || bestScoreText == null || startButton == null || quitButton == null)
            {
                Debug.LogError("TitleScreenController: balanceData・bestScoreText・startButton・quitButton を Inspector で設定してください", this);
                enabled = false;
                return;
            }

            startButton.onClick.AddListener(HandleStartClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
        }

        /// <summary>
        /// 破棄されるときに、ボタンへの登録を外す
        /// </summary>
        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }
        }

        /// <summary>
        /// 最初のフレームの前に、保存されているランキングからベストスコアを読んで表示する
        /// </summary>
        private void Start()
        {
            bestScoreText.text = FormatBestScore(RankingStorage.Load(balanceData.RankingSize).BestScore);
        }

        /// <summary>
        /// 毎フレーム、Enter キー（テンキーの Enter も）で START できるようにする（リザルトの RETRY と同じキー）
        /// </summary>
        private void Update()
        {
            // ローカル変数は関数の先頭で宣言する
            Keyboard keyboard;   // 接続中のキーボード（無ければ null）

            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }

        /// <summary>
        /// ベストスコアの表示を組み立てる（例: BEST  12,500。記録がなければ BEST  ---）
        /// </summary>
        /// <param name="bestScore">ベストスコア（点。記録がなければ 0）</param>
        /// <returns>表示する文字</returns>
        public static string FormatBestScore(int bestScore)
        {
            // 0 点の記録はランキングに残らないので、0 は「まだ記録がない」という意味になる
            if (bestScore <= 0)
            {
                return "BEST  ---";
            }

            return string.Format(CultureInfo.InvariantCulture, "BEST  {0}", HudView.FormatScore(bestScore));
        }

        /// <summary>
        /// START ボタンが押されたときに、ボタンから呼ばれる
        /// </summary>
        private void HandleStartClicked()
        {
            StartGame();
        }

        /// <summary>
        /// Game シーンへ進む（カウントダウン（T11）ができるまでは、すぐに出題から始まる）
        /// </summary>
        private void StartGame()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }

        /// <summary>
        /// 終了ボタンが押されたときに、ボタンから呼ばれる。ビルドではアプリを閉じ、Editor では Console に出すだけ
        /// </summary>
        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            // Editor では Application.Quit が効かないので、押されたことだけを Console に出す
            Debug.Log("TitleScreenController: 終了ボタンが押されました（ビルドではここでアプリを閉じます）", this);
#else
            Application.Quit();
#endif
        }
    }
}
