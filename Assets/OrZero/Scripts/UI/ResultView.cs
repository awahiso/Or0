using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrZero
{
    /// <summary>
    /// リザルト画面（SPEC のリザルト・ランキングの節）。
    /// 見出し（GAME OVER／TIME UP）・SCORE・正解数・RANK、GAME OVER のときの「あなたの回答／正解」、NEW RECORD、
    /// ランキング（上位5件。今回の記録の行は色を変える）を表示し、RETRY・TITLE ボタンが押されたら通知する。
    /// 表示する文字の組み立ては static 関数に分けてあり、EditMode テストで確かめられる
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        // ===== 参照（Inspector で設定） =====
        [SerializeField] private GameObject panel;              // リザルトの画面（表示・非表示を切り替える。このコンポーネントの子に置く）
        [SerializeField] private TMP_Text headingText;          // 見出し（GAME OVER／TIME UP）
        [SerializeField] private TMP_Text answerText;           // GAME OVER のときの「あなたの回答／正解」（TIME UP では空にする）
        [SerializeField] private TMP_Text scoreText;            // SCORE の数値
        [SerializeField] private TMP_Text correctCountText;     // 正解数
        [SerializeField] private TMP_Text rankText;             // RANK（S など）
        [SerializeField] private GameObject newRecordMark;      // NEW RECORD の表示（1位に入ったときだけ出す）
        [SerializeField] private TMP_Text[] rankingRows;        // ランキングの各行（上から1位・2位…の順に並べる）
        [SerializeField] private Button retryButton;            // RETRY ボタン
        [SerializeField] private Button titleButton;            // TITLE ボタン

        // ===== 見た目の設定（Inspector で調整） =====
        [SerializeField] private Color rankingNormalColor = new Color32(0x1F, 0x20, 0x26, 0xFF);      // ランキングの行の文字色（SPEC §1.5 の文字の色）
        [SerializeField] private Color rankingHighlightColor = new Color32(0xFF, 0x4F, 0x9A, 0xFF);   // 今回の記録の行の文字色（SPEC §1.5 のピンク）

        /// <summary>RETRY ボタンが押されたときに呼ばれる</summary>
        public event Action RetryPressed;

        /// <summary>TITLE ボタンが押されたときに呼ばれる</summary>
        public event Action TitlePressed;

        /// <summary>
        /// RETRY・TITLE ボタンが押されたら通知するように登録する
        /// </summary>
        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetryClicked);
            }
            if (titleButton != null)
            {
                titleButton.onClick.AddListener(HandleTitleClicked);
            }
        }

        /// <summary>
        /// 破棄されるときに、ボタンへの登録を外す
        /// </summary>
        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(HandleRetryClicked);
            }
            if (titleButton != null)
            {
                titleButton.onClick.RemoveListener(HandleTitleClicked);
            }
        }

        /// <summary>
        /// Inspector の設定漏れがないか確かめる（GameFlowController が起動時に呼ぶ）。漏れがあれば Console にエラーを出す
        /// </summary>
        /// <returns>漏れがなければ true</returns>
        public bool HasValidReferences()
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字

            if (panel == null || headingText == null || answerText == null || scoreText == null
                || correctCountText == null || rankText == null || newRecordMark == null || retryButton == null
                || titleButton == null || rankingRows == null || rankingRows.Length == 0)
            {
                Debug.LogError("ResultView: panel・各テキスト・newRecordMark・rankingRows・retryButton・titleButton を Inspector で設定してください", this);
                return false;
            }
            for (i = 0; i < rankingRows.Length; i++)
            {
                if (rankingRows[i] == null)
                {
                    Debug.LogError($"ResultView: rankingRows の {i} 番目が未設定です", this);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// リザルト画面を隠す（プレイ中）
        /// </summary>
        public void Hide()
        {
            panel.SetActive(false);
        }

        /// <summary>
        /// 1プレイの結果とランキングを表示する
        /// </summary>
        /// <param name="data">リザルト画面に出す内容</param>
        public void Show(ResultData data)
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ランキングの行の添字

            // 表示する内容がないのは呼び出し側のミスなので止める
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            headingText.text = FormatHeading(data.EndReason);
            answerText.text = data.EndReason == GameEndReason.Miss ? FormatAnswerLine(data.WrongAnswer, data.CorrectAnswer) : "";
            scoreText.text = HudView.FormatScore(data.Score);
            correctCountText.text = data.CorrectCount.ToString(CultureInfo.InvariantCulture);
            rankText.text = data.RankName;
            newRecordMark.SetActive(data.IsNewRecord);

            // ランキング: 記録のある行は「順位・ランク・スコア」、ない行はダッシュ。今回の記録の行だけ色を変える
            for (i = 0; i < rankingRows.Length; i++)
            {
                if (i < data.RankingScores.Count)
                {
                    rankingRows[i].text = FormatRankingRow(i + 1, data.RankingRanks[i], data.RankingScores[i]);
                }
                else
                {
                    rankingRows[i].text = FormatEmptyRankingRow(i + 1);
                }
                rankingRows[i].color = i == data.RankingPosition ? rankingHighlightColor : rankingNormalColor;
            }

            panel.SetActive(true);
        }

        /// <summary>
        /// 見出しを組み立てる（GAME OVER／TIME UP）
        /// </summary>
        /// <param name="reason">終わった理由</param>
        /// <returns>見出しの文字</returns>
        public static string FormatHeading(GameEndReason reason)
        {
            return reason == GameEndReason.Miss ? "GAME OVER" : "TIME UP";
        }

        /// <summary>
        /// GAME OVER のときの「あなたの回答／正解」を組み立てる（例: あなたの回答：英字のO　正解：数字の0）
        /// </summary>
        /// <param name="wrongAnswer">押した答え</param>
        /// <param name="correctAnswer">正解</param>
        /// <returns>表示する文字</returns>
        public static string FormatAnswerLine(GlyphType wrongAnswer, GlyphType correctAnswer)
        {
            return $"あなたの回答：{FormatAnswerName(wrongAnswer)}　正解：{FormatAnswerName(correctAnswer)}";
        }

        /// <summary>
        /// ランキングの1行を組み立てる（例: 1位　S　12,500）
        /// </summary>
        /// <param name="place">順位（1 が1位）</param>
        /// <param name="rankName">そのスコアのランク</param>
        /// <param name="score">スコア（点）</param>
        /// <returns>表示する文字</returns>
        public static string FormatRankingRow(int place, string rankName, int score)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}位　{1}　{2}", place, rankName, HudView.FormatScore(score));
        }

        /// <summary>
        /// 記録がまだない行を組み立てる（例: 4位　―）
        /// </summary>
        /// <param name="place">順位（1 が1位）</param>
        /// <returns>表示する文字</returns>
        public static string FormatEmptyRankingRow(int place)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}位　―", place);
        }

        /// <summary>
        /// 答えの種類を、リザルトに出す名前にする（英字のO／数字の0）
        /// </summary>
        /// <param name="answer">答えの種類</param>
        /// <returns>表示する名前</returns>
        private static string FormatAnswerName(GlyphType answer)
        {
            return answer == GlyphType.LetterO ? "英字のO" : "数字の0";
        }

        /// <summary>
        /// RETRY ボタンが押されたときに、ボタンから呼ばれる
        /// </summary>
        private void HandleRetryClicked()
        {
            RetryPressed?.Invoke();
        }

        /// <summary>
        /// TITLE ボタンが押されたときに、ボタンから呼ばれる
        /// </summary>
        private void HandleTitleClicked()
        {
            TitlePressed?.Invoke();
        }
    }
}
