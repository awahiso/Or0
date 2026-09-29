using System.Globalization;
using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// HudView の表示形式のテスト（EditMode）
    /// SPEC §1.1「TIME は小数2桁、第3位は切り上げ（0.00 と表示されたら必ず時間切れ）」と
    /// SPEC §5「数値の表示は OS の地域設定に左右されない」を確かめる
    /// </summary>
    public class HudViewTests
    {
        /// <summary>
        /// 残り時間は 1/100 秒単位に切り上げる（時間が少しでも残っていれば 0.00 と表示しない）
        /// </summary>
        [Test]
        public void ToCentiseconds_VariousSeconds_RoundsUp()
        {
            // 実行・確認
            Assert.AreEqual(6000, HudView.ToCentiseconds(60f));      // ちょうど 60.00
            Assert.AreEqual(9999, HudView.ToCentiseconds(99.99f));   // 上限 99.99 がそのまま出る
            Assert.AreEqual(1, HudView.ToCentiseconds(0.001f));      // わずかに残っていれば 0.01
            Assert.AreEqual(0, HudView.ToCentiseconds(0f));          // 0.00 は時間切れのときだけ
            Assert.AreEqual(0, HudView.ToCentiseconds(-1f));         // マイナスは 0 として扱う
        }

        /// <summary>
        /// TIME は整数部と、小さく表示する小数部（2桁）に分けて組み立てる
        /// </summary>
        [Test]
        public void FormatTime_4231_ShowsFractionSmaller()
        {
            // 実行・確認: 42.31秒、小数部は 60% の大きさ
            Assert.AreEqual("42<size=60%>.31</size>", HudView.FormatTime(4231, 60));
        }

        /// <summary>
        /// 小数部が1桁のときは 0 で埋めて2桁にする（1.05 が 1.5 にならない）
        /// </summary>
        [Test]
        public void FormatTime_105_PadsFractionWithZero()
        {
            // 実行・確認
            Assert.AreEqual("1<size=60%>.05</size>", HudView.FormatTime(105, 60));
        }

        /// <summary>
        /// SCORE は3桁ごとにカンマで区切る
        /// </summary>
        [Test]
        public void FormatScore_12500_UsesCommaSeparator()
        {
            // 実行・確認
            Assert.AreEqual("12,500", HudView.FormatScore(12500));
        }

        /// <summary>
        /// OS の地域設定がドイツ語（桁区切りが「.」の地域）でも、SCORE と TIME の表記が変わらない
        /// </summary>
        [Test]
        public void Format_GermanCulture_KeepsSameNotation()
        {
            // ローカル変数は関数の先頭で宣言する
            CultureInfo originalCulture;   // テスト前の地域設定（最後に戻す）
            string score;                  // SCORE の表示
            string time;                   // TIME の表示

            originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // 準備: 地域設定をドイツ語にする
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                // 実行
                score = HudView.FormatScore(12500);
                time = HudView.FormatTime(4231, 60);

                // 確認: 「12.500」「42,31」のようにならない
                Assert.AreEqual("12,500", score);
                Assert.AreEqual("42<size=60%>.31</size>", time);
            }
            finally
            {
                // 後のテストに影響しないよう、地域設定を元に戻す
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// COMBO は「COMBO × 数」の形で出す
        /// </summary>
        [Test]
        public void FormatCombo_23_ShowsComboTimesCount()
        {
            // 実行・確認
            Assert.AreEqual("COMBO × 23", HudView.FormatCombo(23));
        }
    }
}
