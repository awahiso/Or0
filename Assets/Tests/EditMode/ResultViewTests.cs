using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// ResultView（リザルト画面）の、表示する文字の組み立てのテスト（EditMode）
    /// 見出し・「あなたの回答／正解」・ランキングの行の形を確かめる
    /// </summary>
    public class ResultViewTests
    {
        /// <summary>
        /// 終わった理由ごとに見出しが変わる
        /// </summary>
        [Test]
        public void FormatHeading_EachReason_ReturnsHeading()
        {
            // 実行・確認
            Assert.AreEqual("GAME OVER", ResultView.FormatHeading(GameEndReason.Miss));
            Assert.AreEqual("TIME UP", ResultView.FormatHeading(GameEndReason.TimeUp));
        }

        /// <summary>
        /// 英字を押して数字が正解だったとき、両方が文字で出る（SPEC の例どおり）
        /// </summary>
        [Test]
        public void FormatAnswerLine_PressedLetterButDigit_ShowsBoth()
        {
            // 実行・確認
            Assert.AreEqual("あなたの回答：英字のO　正解：数字の0", ResultView.FormatAnswerLine(GlyphType.LetterO, GlyphType.DigitZero));
        }

        /// <summary>
        /// 数字を押して英字が正解だったとき、両方が文字で出る
        /// </summary>
        [Test]
        public void FormatAnswerLine_PressedDigitButLetter_ShowsBoth()
        {
            // 実行・確認
            Assert.AreEqual("あなたの回答：数字の0　正解：英字のO", ResultView.FormatAnswerLine(GlyphType.DigitZero, GlyphType.LetterO));
        }

        /// <summary>
        /// ランキングの行は「順位・ランク・スコア（3桁ごとにカンマ）」
        /// </summary>
        [Test]
        public void FormatRankingRow_PlaceRankScore_FormatsWithComma()
        {
            // 実行・確認
            Assert.AreEqual("1位　S　12,500", ResultView.FormatRankingRow(1, "S", 12500));
            Assert.AreEqual("5位　D　190", ResultView.FormatRankingRow(5, "D", 190));
        }

        /// <summary>
        /// 記録がまだない行は、順位とダッシュだけを出す
        /// </summary>
        [Test]
        public void FormatEmptyRankingRow_NoRecord_ShowsDash()
        {
            // 実行・確認
            Assert.AreEqual("4位　―", ResultView.FormatEmptyRankingRow(4));
        }
    }
}
