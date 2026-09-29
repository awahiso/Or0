using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// 画面上部の HUD（SCORE・TIME・COMBO）を表示するクラス（SPEC §1.2）。
    /// 数値の表示は OS の地域設定に左右されないよう、InvariantCulture で組み立てる（SPEC §5）。
    /// 表示形式の組み立ては static 関数に分けてあり、EditMode テストで確かめられる
    /// </summary>
    public class HudView : MonoBehaviour
    {
        // ===== 参照・見た目の設定（Inspector で設定） =====
        [SerializeField] private TMP_Text scoreText;   // SCORE の数値
        [SerializeField] private TMP_Text timeText;    // TIME の数値（整数部は大きく、小数部は小さく）
        [SerializeField] private TMP_Text comboText;   // COMBO × N
        [SerializeField, Range(10, 100)] private int timeFractionSizePercent = 60;   // TIME の小数部の大きさ（整数部に対する %）

        /// <summary>
        /// SCORE を表示する
        /// </summary>
        /// <param name="score">現在のスコア（点）</param>
        public void ShowScore(int score)
        {
            scoreText.text = FormatScore(score);
        }

        /// <summary>
        /// TIME を表示する（小数2桁。第3位は切り上げ）
        /// </summary>
        /// <param name="remainingSeconds">残り時間（秒）</param>
        public void ShowTime(float remainingSeconds)
        {
            timeText.text = FormatTime(ToCentiseconds(remainingSeconds), timeFractionSizePercent);
        }

        /// <summary>
        /// COMBO を表示する
        /// </summary>
        /// <param name="combo">現在のコンボ数（＝正解数）</param>
        public void ShowCombo(int combo)
        {
            comboText.text = FormatCombo(combo);
        }

        /// <summary>
        /// 残り時間を、表示用に 1/100 秒単位へ切り上げる。
        /// 少しでも時間が残っていれば 0.01 以上になるので、「0.00」が出るのは時間切れのときだけになる（SPEC §1.1）
        /// </summary>
        /// <param name="seconds">残り時間（秒）</param>
        /// <returns>1/100 秒単位の値（例: 42.305秒 → 4231）</returns>
        public static int ToCentiseconds(float seconds)
        {
            // マイナスは 0 として扱い、double で計算して切り上げる
            return (int)Math.Ceiling(Math.Max(0f, seconds) * 100.0);
        }

        /// <summary>
        /// TIME の表示文字列を組み立てる（例: 4231 → 「42」＋小さめの「.31」）
        /// </summary>
        /// <param name="centiseconds">1/100 秒単位の残り時間</param>
        /// <param name="fractionSizePercent">小数部の大きさ（整数部に対する %）</param>
        /// <returns>TMP のリッチテキスト</returns>
        public static string FormatTime(int centiseconds, int fractionSizePercent)
        {
            // ローカル変数は関数の先頭で宣言する
            int wholeSeconds;   // 整数部（秒）
            int fraction;       // 小数部（1/100 秒）

            wholeSeconds = centiseconds / 100;
            fraction = centiseconds % 100;

            // 小数点は地域設定に関係なく「.」、小数部は必ず2桁（1.05 が 1.5 にならないように）
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}<size={1}%>.{2:00}</size>",
                wholeSeconds, fractionSizePercent, fraction);
        }

        /// <summary>
        /// SCORE の表示文字列を組み立てる（3桁ごとにカンマ。例: 12500 → 12,500）
        /// </summary>
        /// <param name="score">スコア（点）</param>
        /// <returns>表示文字列</returns>
        public static string FormatScore(int score)
        {
            return score.ToString("#,0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// COMBO の表示文字列を組み立てる（例: 23 → COMBO × 23）
        /// </summary>
        /// <param name="combo">コンボ数</param>
        /// <returns>表示文字列</returns>
        public static string FormatCombo(int combo)
        {
            return string.Format(CultureInfo.InvariantCulture, "COMBO × {0}", combo);
        }
    }
}
