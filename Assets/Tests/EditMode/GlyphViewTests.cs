using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// GlyphView の切り替わり演出（出題のたびに一瞬小さくして戻す）の大きさ計算のテスト（EditMode）
    /// SPEC §1.2「縦横同じ比率で変えるので、公平性には影響しない」演出の中身を確かめる
    /// </summary>
    public class GlyphViewTests
    {
        // ===== テストで共通に使う値 =====
        private const float Tolerance = 0.0001f;   // float の比較で許す誤差
        private const float StartScale = 0.85f;    // 出題した瞬間の大きさ（元の大きさに対する倍率）
        private const float Duration = 0.08f;      // 元の大きさに戻るまでの時間（秒）

        /// <summary>
        /// 出題した瞬間は、開始時の大きさ（0.85倍）
        /// </summary>
        [Test]
        public void CalculatePopScale_AtStart_ReturnsStartScale()
        {
            // 実行・確認
            Assert.AreEqual(StartScale, GlyphView.CalculatePopScale(0f, Duration, StartScale), Tolerance);
        }

        /// <summary>
        /// 時間が過ぎたら元の大きさ（1倍）で止まり、それ以上は大きくならない
        /// </summary>
        [Test]
        public void CalculatePopScale_AfterDuration_ReturnsOne()
        {
            // 実行・確認
            Assert.AreEqual(1f, GlyphView.CalculatePopScale(Duration, Duration, StartScale), Tolerance);
            Assert.AreEqual(1f, GlyphView.CalculatePopScale(1f, Duration, StartScale), Tolerance);
        }

        /// <summary>
        /// 途中は、最初に速く・最後にゆっくり元の大きさへ近づく（半分の時間で残りの4分の3まで戻る）
        /// </summary>
        [Test]
        public void CalculatePopScale_Halfway_EasesOut()
        {
            // 実行・確認: 0.85 + (1 − 0.85) × 0.75 = 0.9625
            Assert.AreEqual(0.9625f, GlyphView.CalculatePopScale(Duration * 0.5f, Duration, StartScale), Tolerance);
        }

        /// <summary>
        /// 演出の時間が 0 なら、演出なしで最初から元の大きさ
        /// </summary>
        [Test]
        public void CalculatePopScale_ZeroDuration_ReturnsOne()
        {
            // 実行・確認
            Assert.AreEqual(1f, GlyphView.CalculatePopScale(0f, 0f, StartScale), Tolerance);
        }
    }
}
