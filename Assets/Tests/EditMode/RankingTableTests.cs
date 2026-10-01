using System;
using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// RankingTable（ランキング上位 N 件）のテスト（EditMode）
    /// SPEC のランキングの決まり（スコアの高い順・同点は先に出した記録が上・0点は残さない・N 件まで・1位なら NEW RECORD）と、
    /// 保存用の JSON の読み書きを確かめる
    /// </summary>
    public class RankingTableTests
    {
        // ===== テストで共通に使う値 =====
        private const int Size = 5;   // ランキングに残す件数（件）。SPEC の既定値

        /// <summary>
        /// 空のランキングに入れた最初の記録は1位（0 番）になる
        /// </summary>
        [Test]
        public void Insert_IntoEmpty_ReturnsFirstPlace()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象
            int position;         // 入った順位（0 が1位）

            // 準備・実行
            table = new RankingTable(Size);
            position = table.Insert(1000);

            // 確認
            Assert.AreEqual(0, position);
            CollectionAssert.AreEqual(new[] { 1000 }, table.Scores);
        }

        /// <summary>
        /// どんな順で入れても、スコアの高い順に並ぶ。戻り値は入った位置
        /// </summary>
        [Test]
        public void Insert_VariousScores_KeepsDescendingOrder()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象

            // 準備
            table = new RankingTable(Size);

            // 実行・確認: 500 → 1位、1500 → 1位、1000 → 2位
            Assert.AreEqual(0, table.Insert(500));
            Assert.AreEqual(0, table.Insert(1500));
            Assert.AreEqual(1, table.Insert(1000));
            CollectionAssert.AreEqual(new[] { 1500, 1000, 500 }, table.Scores);
        }

        /// <summary>
        /// 同じスコアは、先に出した記録の下に入る
        /// </summary>
        [Test]
        public void Insert_SameScore_GoesBelowEarlierRecord()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象
            int position;         // 入った順位

            // 準備: 1000 と 500 が入っている
            table = CreateTable(1000, 500);

            // 実行: もう一度 1000
            position = table.Insert(1000);

            // 確認: 先に出した 1000 の下（2位）に入る
            Assert.AreEqual(1, position);
            CollectionAssert.AreEqual(new[] { 1000, 1000, 500 }, table.Scores);
        }

        /// <summary>
        /// 1位（NEW RECORD）になるのは、それまでの1位より高いときだけ。同点は2位
        /// </summary>
        [Test]
        public void Insert_AgainstBest_FirstPlaceOnlyWhenHigher()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象

            // 準備: 1位が 3000
            table = CreateTable(3000);

            // 実行・確認
            Assert.AreEqual(1, table.Insert(3000));   // 同点 → 2位（NEW RECORD ではない）
            Assert.AreEqual(0, table.Insert(3001));   // 1点でも上 → 1位（NEW RECORD）
        }

        /// <summary>
        /// 5件そろっているときに5位より高い記録が入ると、いちばん下の記録が押し出される
        /// </summary>
        [Test]
        public void Insert_WhenFull_DropsLowestRecord()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象
            int position;         // 入った順位

            // 準備
            table = CreateTable(5000, 4000, 3000, 2000, 1000);

            // 実行
            position = table.Insert(3500);

            // 確認: 3位に入り、1000 が外れる
            Assert.AreEqual(2, position);
            CollectionAssert.AreEqual(new[] { 5000, 4000, 3500, 3000, 2000 }, table.Scores);
        }

        /// <summary>
        /// 5件そろっているとき、5位以下（5位と同点を含む）の記録は入らず、ランキングも変わらない
        /// </summary>
        [Test]
        public void Insert_WhenFullAndNotAboveLowest_NotRecorded()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象

            // 準備
            table = CreateTable(5000, 4000, 3000, 2000, 1000);

            // 実行・確認: 5位と同点は、先に出した記録が上なので6位（圏外）
            Assert.AreEqual(-1, table.Insert(1000));
            Assert.AreEqual(-1, table.Insert(999));
            CollectionAssert.AreEqual(new[] { 5000, 4000, 3000, 2000, 1000 }, table.Scores);
        }

        /// <summary>
        /// 0点以下の記録は残さない
        /// </summary>
        [Test]
        public void Insert_ZeroOrNegative_NotRecorded()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象

            // 準備
            table = new RankingTable(Size);

            // 実行・確認
            Assert.AreEqual(-1, table.Insert(0));
            Assert.AreEqual(-1, table.Insert(-5));
            Assert.AreEqual(0, table.Scores.Count);
        }

        /// <summary>
        /// ベストスコアは1位のスコア。記録がなければ 0
        /// </summary>
        [Test]
        public void BestScore_EmptyAndAfterInsert_ReturnsTopScore()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // テスト対象

            // 準備
            table = new RankingTable(Size);

            // 実行・確認
            Assert.AreEqual(0, table.BestScore);
            table.Insert(800);
            table.Insert(1200);
            Assert.AreEqual(1200, table.BestScore);
        }

        /// <summary>
        /// JSON にして戻すと、スコアと並び（同点の順も）がそのまま戻る
        /// </summary>
        [Test]
        public void ToJsonAndFromJson_RoundTrip_KeepsScoresAndOrder()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;      // 元のランキング
            RankingTable restored;   // JSON から戻したランキング

            // 準備
            table = CreateTable(1500, 1000, 1000);

            // 実行
            restored = RankingTable.FromJson(table.ToJson(), Size);

            // 確認
            CollectionAssert.AreEqual(table.Scores, restored.Scores);
        }

        /// <summary>
        /// 保存データがまだない（空の文字）なら、空のランキングから始める
        /// </summary>
        [Test]
        public void FromJson_NullOrEmpty_StartsEmpty()
        {
            // 実行・確認
            Assert.AreEqual(0, RankingTable.FromJson(null, Size).Scores.Count);
            Assert.AreEqual(0, RankingTable.FromJson("", Size).Scores.Count);
        }

        /// <summary>
        /// 並びが崩れた・0点以下が混ざった・件数が多すぎる保存データも、決まりどおりに整えて読む
        /// （保存の形式は {"scores":[...]}。この形を変えると今までのデータが読めなくなる）
        /// </summary>
        [Test]
        public void FromJson_MessyData_IsCleanedUp()
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // 読み込んだランキング

            // 実行
            table = RankingTable.FromJson("{\"scores\":[100,0,3000,-5,2000,900,800,700]}", Size);

            // 確認: 高い順・0点以下を外す・5件まで
            CollectionAssert.AreEqual(new[] { 3000, 2000, 900, 800, 700 }, table.Scores);
        }

        /// <summary>
        /// 壊れた保存データは ArgumentException になる（読み込む側で、空から始めるなどの扱いを決める）
        /// </summary>
        [Test]
        public void FromJson_BrokenText_ThrowsArgumentException()
        {
            // 実行・確認
            Assert.Throws<ArgumentException>(() => RankingTable.FromJson("{broken", Size));
        }

        /// <summary>
        /// 残す件数が 1 未満だと、ランキングにならないので例外になる
        /// </summary>
        [Test]
        public void Constructor_SizeBelowOne_ThrowsArgumentOutOfRangeException()
        {
            // 実行・確認
            Assert.Throws<ArgumentOutOfRangeException>(() => new RankingTable(0));
        }

        /// <summary>
        /// テスト用に、記録を順に入れたランキングを作る
        /// </summary>
        /// <param name="scores">入れるスコア（この順に入れる）</param>
        /// <returns>作ったランキング</returns>
        private static RankingTable CreateTable(params int[] scores)
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;   // 作るランキング
            int i;                // ループ用の添字

            table = new RankingTable(Size);
            for (i = 0; i < scores.Length; i++)
            {
                table.Insert(scores[i]);
            }
            return table;
        }
    }
}
