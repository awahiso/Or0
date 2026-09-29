using System;
using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// ScoreCalculator（1問の獲得点）のテスト（EditMode）
    /// SPEC §1.3「1問の獲得点 = 基本点 + スピード加点（整数に四捨五入）」を確かめる
    /// </summary>
    public class ScoreCalculatorTests
    {
        // ===== テストで共通に使う値（SPEC の初期値） =====
        private const int BasePoint = 100;       // 基本点（点）
        private const int SpeedBonusMax = 100;   // スピード加点の最大（点）
        private const float Window = 1.0f;      // スピード加点がもらえる猶予（秒）

        /// <summary>
        /// 0秒で答えると、スピード加点が満点になる（100 + 100 = 200点）
        /// </summary>
        [Test]
        public void CalculatePoint_ZeroSeconds_FullSpeedBonus()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, 0f);

            // 確認
            Assert.AreEqual(200, point);
        }

        /// <summary>
        /// 0.4秒で答えると、スピード加点は60点（100 + 60 = 160点。SPEC §1.3 の例）
        /// </summary>
        [Test]
        public void CalculatePoint_ZeroPointFourSeconds_Returns160()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, 0.4f);

            // 確認
            Assert.AreEqual(160, point);
        }

        /// <summary>
        /// 0.8秒で答えると120点。浮動小数点の誤差で119点にならない（四捨五入の確認。SPEC §1.3 の例）
        /// </summary>
        [Test]
        public void CalculatePoint_ZeroPointEightSeconds_Returns120()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, 0.8f);

            // 確認
            Assert.AreEqual(120, point);
        }

        /// <summary>
        /// 猶予ちょうど（1.0秒）で答えると、スピード加点は0（基本点のみ）
        /// </summary>
        [Test]
        public void CalculatePoint_ExactlyWindow_NoSpeedBonus()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, 1.0f);

            // 確認
            Assert.AreEqual(100, point);
        }

        /// <summary>
        /// 回答時間が猶予を超えたら、スピード加点は0になる（マイナスにならない）
        /// </summary>
        [Test]
        public void CalculatePoint_SlowerThanWindow_NoSpeedBonus()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行: 1.5秒かけて回答
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, 1.5f);

            // 確認: 基本点のみになる
            Assert.AreEqual(100, point);
        }

        /// <summary>
        /// 回答時間がマイナスでも、スピード加点は最大を超えない
        /// </summary>
        [Test]
        public void CalculatePoint_NegativeSeconds_BonusCappedAtMax()
        {
            // ローカル変数は関数の先頭で宣言する
            int point;   // 獲得点

            // 実行
            point = ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, Window, -1f);

            // 確認: 100 + 100 = 200点で止まる
            Assert.AreEqual(200, point);
        }

        /// <summary>
        /// 猶予が0以下だと割り算ができないので、設定ミスとして例外になる
        /// </summary>
        [Test]
        public void CalculatePoint_ZeroWindow_ThrowsArgumentOutOfRangeException()
        {
            // 実行・確認
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreCalculator.CalculatePoint(BasePoint, SpeedBonusMax, 0f, 0.5f));
        }
    }
}
