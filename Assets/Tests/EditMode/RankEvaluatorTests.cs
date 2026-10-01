using System;
using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// RankEvaluator（スコアからランクを決める）のテスト（EditMode）
    /// SPEC §6「RankEvaluator: 各閾値の境界」を確かめる。境目は SPEC の既定値（S 10,000／A 6,500／B 3,500／C 1,500／D 0）
    /// </summary>
    public class RankEvaluatorTests
    {
        /// <summary>
        /// 境目ちょうどのスコアは、そのランクになる
        /// </summary>
        [Test]
        public void Evaluate_ExactlyAtThreshold_ReturnsThatRank()
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold[] thresholds;   // ランクの境目

            // 準備
            thresholds = CreateDefaultThresholds();

            // 実行・確認
            Assert.AreEqual("S", RankEvaluator.Evaluate(10000, thresholds));
            Assert.AreEqual("A", RankEvaluator.Evaluate(6500, thresholds));
            Assert.AreEqual("B", RankEvaluator.Evaluate(3500, thresholds));
            Assert.AreEqual("C", RankEvaluator.Evaluate(1500, thresholds));
            Assert.AreEqual("D", RankEvaluator.Evaluate(0, thresholds));
        }

        /// <summary>
        /// 境目の1点下は、1つ下のランクになる
        /// </summary>
        [Test]
        public void Evaluate_OnePointBelowThreshold_ReturnsNextRank()
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold[] thresholds;   // ランクの境目

            // 準備
            thresholds = CreateDefaultThresholds();

            // 実行・確認
            Assert.AreEqual("A", RankEvaluator.Evaluate(9999, thresholds));
            Assert.AreEqual("B", RankEvaluator.Evaluate(6499, thresholds));
            Assert.AreEqual("C", RankEvaluator.Evaluate(3499, thresholds));
            Assert.AreEqual("D", RankEvaluator.Evaluate(1499, thresholds));
        }

        /// <summary>
        /// いちばん上の境目をはるかに超えても、いちばん上のランクのまま（コンセプト画像の 12,500 も S）
        /// </summary>
        [Test]
        public void Evaluate_FarAboveTopThreshold_ReturnsTopRank()
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold[] thresholds;   // ランクの境目

            // 準備
            thresholds = CreateDefaultThresholds();

            // 実行・確認
            Assert.AreEqual("S", RankEvaluator.Evaluate(12500, thresholds));
            Assert.AreEqual("S", RankEvaluator.Evaluate(int.MaxValue, thresholds));
        }

        /// <summary>
        /// 一覧の並び順が逆でもばらばらでも、結果は同じ（Inspector での並べ替えミスに左右されない）
        /// </summary>
        [Test]
        public void Evaluate_ShuffledThresholds_SameResultAsOrdered()
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold[] ordered;    // 高い順に並べた境目
            RankThreshold[] shuffled;   // 並びをばらばらにした境目
            int[] scores;               // 確かめるスコア
            int i;                      // ループ用の添字

            // 準備
            ordered = CreateDefaultThresholds();
            shuffled = new[]
            {
                new RankThreshold("C", 1500),
                new RankThreshold("S", 10000),
                new RankThreshold("D", 0),
                new RankThreshold("B", 3500),
                new RankThreshold("A", 6500),
            };
            scores = new[] { 0, 1499, 1500, 3499, 3500, 6499, 6500, 9999, 10000, 12500 };

            // 実行・確認
            for (i = 0; i < scores.Length; i++)
            {
                Assert.AreEqual(RankEvaluator.Evaluate(scores[i], ordered), RankEvaluator.Evaluate(scores[i], shuffled));
            }
        }

        /// <summary>
        /// どの境目にも届かないスコア（境目に 0 がない設定や、マイナスのスコア）は、いちばん下のランクになる
        /// </summary>
        [Test]
        public void Evaluate_BelowAllThresholds_ReturnsLowestRank()
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold[] noZero;   // いちばん下が C（1,500）で、0 の境目がない設定

            // 準備
            noZero = new[] { new RankThreshold("S", 10000), new RankThreshold("C", 1500) };

            // 実行・確認
            Assert.AreEqual("C", RankEvaluator.Evaluate(100, noZero));
            Assert.AreEqual("D", RankEvaluator.Evaluate(-1, CreateDefaultThresholds()));
        }

        /// <summary>
        /// 境目が1つもない・一覧が null・空の要素があるときは、ランクを決められないので例外になる
        /// </summary>
        [Test]
        public void Evaluate_InvalidThresholds_Throws()
        {
            // 実行・確認
            Assert.Throws<ArgumentException>(() => RankEvaluator.Evaluate(100, new RankThreshold[0]));
            Assert.Throws<ArgumentNullException>(() => RankEvaluator.Evaluate(100, null));
            Assert.Throws<ArgumentException>(() => RankEvaluator.Evaluate(100, new RankThreshold[] { new RankThreshold("S", 10000), null }));
        }

        /// <summary>
        /// テスト用に、SPEC の既定値と同じ境目を作る（高い順）
        /// </summary>
        /// <returns>ランクの境目</returns>
        private static RankThreshold[] CreateDefaultThresholds()
        {
            return new[]
            {
                new RankThreshold("S", 10000),
                new RankThreshold("A", 6500),
                new RankThreshold("B", 3500),
                new RankThreshold("C", 1500),
                new RankThreshold("D", 0),
            };
        }
    }
}
