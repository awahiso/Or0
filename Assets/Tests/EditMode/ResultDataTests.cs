using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// ResultData（リザルト画面に出す内容）のテスト（EditMode）
    /// NEW RECORD の判定（1位のときだけ）と、入力の確認・作ったあとに中身が変わらないことを確かめる
    /// </summary>
    public class ResultDataTests
    {
        /// <summary>
        /// NEW RECORD になるのは、ランキングの1位（0 番）に入ったときだけ
        /// </summary>
        [Test]
        public void IsNewRecord_OnlyWhenFirstPlace()
        {
            // 実行・確認
            Assert.IsTrue(CreateData(0).IsNewRecord);
            Assert.IsFalse(CreateData(2).IsNewRecord);
            Assert.IsFalse(CreateData(-1).IsNewRecord);
        }

        /// <summary>
        /// ランキングのスコアとランクの数がそろっていないと、行を作れないので例外になる
        /// </summary>
        [Test]
        public void Constructor_RankingLengthMismatch_ThrowsArgumentException()
        {
            // 実行・確認
            Assert.Throws<ArgumentException>(() => new ResultData(
                GameEndReason.Miss, 1000, 5, "D", GlyphType.LetterO, GlyphType.DigitZero,
                new[] { 1000, 500 }, new[] { "D" }, 0));
        }

        /// <summary>
        /// ランキングが null なら例外になる（記録がないときは空の一覧を渡す）
        /// </summary>
        [Test]
        public void Constructor_NullRanking_ThrowsArgumentNullException()
        {
            // 実行・確認
            Assert.Throws<ArgumentNullException>(() => new ResultData(
                GameEndReason.Miss, 1000, 5, "D", GlyphType.LetterO, GlyphType.DigitZero,
                null, new string[0], -1));
        }

        /// <summary>
        /// 作ったあとで元の一覧を書き換えても、中身は変わらない（写しを持つ）
        /// </summary>
        [Test]
        public void Constructor_SourceListChangedLater_KeepsOriginalValues()
        {
            // ローカル変数は関数の先頭で宣言する
            List<int> scores;    // 渡すスコアの一覧
            List<string> ranks;  // 渡すランクの一覧
            ResultData data;     // テスト対象

            // 準備
            scores = new List<int> { 12500, 9000 };
            ranks = new List<string> { "S", "A" };
            data = new ResultData(GameEndReason.TimeUp, 12500, 70, "S", GlyphType.LetterO, GlyphType.LetterO, scores, ranks, 0);

            // 実行: 作ったあとで元の一覧を書き換える
            scores[0] = 1;
            ranks[0] = "D";

            // 確認
            Assert.AreEqual(12500, data.RankingScores[0]);
            Assert.AreEqual("S", data.RankingRanks[0]);
        }

        /// <summary>
        /// テスト用に、順位だけを変えたリザルトの内容を作る
        /// </summary>
        /// <param name="rankingPosition">今回の記録の順位（0 が1位。-1 はランキング外）</param>
        /// <returns>作った内容</returns>
        private static ResultData CreateData(int rankingPosition)
        {
            return new ResultData(GameEndReason.Miss, 1000, 5, "D", GlyphType.LetterO, GlyphType.DigitZero,
                new[] { 3000, 2000, 1000 }, new[] { "C", "D", "D" }, rankingPosition);
        }
    }
}
