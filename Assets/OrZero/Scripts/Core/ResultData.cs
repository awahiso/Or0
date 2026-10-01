using System;
using System.Collections.Generic;

namespace OrZero
{
    /// <summary>
    /// リザルト画面に出す内容（1プレイの結果とランキング）。GameFlowController が作り、ResultView が表示する。
    /// 作ったあとは中身を変えない（渡された一覧は写して持つ）
    /// </summary>
    public class ResultData
    {
        // ===== 結果（作るときに決まり、あとから変えない） =====
        private readonly GameEndReason endReason;   // 終わった理由
        private readonly int score;                 // スコア（点）
        private readonly int correctCount;          // 正解数（問）
        private readonly string rankName;           // 今回のランク（S など）
        private readonly GlyphType wrongAnswer;     // ミスしたときに押した答え（TIME UP では使わない）
        private readonly GlyphType correctAnswer;   // ミスしたときの正解（TIME UP では使わない）
        private readonly int[] rankingScores;       // ランキングのスコア（点。高い順）
        private readonly string[] rankingRanks;     // ランキングの各スコアのランク（rankingScores と同じ並び）
        private readonly int rankingPosition;       // 今回の記録の順位（0 が1位＝NEW RECORD。-1 はランキング外）

        /// <summary>
        /// リザルト画面に出す内容を作る
        /// </summary>
        /// <param name="endReason">終わった理由</param>
        /// <param name="score">スコア（点）</param>
        /// <param name="correctCount">正解数（問）</param>
        /// <param name="rankName">今回のランク</param>
        /// <param name="wrongAnswer">ミスしたときに押した答え</param>
        /// <param name="correctAnswer">ミスしたときの正解</param>
        /// <param name="rankingScores">ランキングのスコア（高い順。記録がなければ空）</param>
        /// <param name="rankingRanks">ランキングの各スコアのランク（rankingScores と同じ数）</param>
        /// <param name="rankingPosition">今回の記録の順位（0 が1位。-1 はランキング外）</param>
        public ResultData(GameEndReason endReason, int score, int correctCount, string rankName,
                          GlyphType wrongAnswer, GlyphType correctAnswer,
                          IReadOnlyList<int> rankingScores, IReadOnlyList<string> rankingRanks, int rankingPosition)
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字

            // ランキングの一覧がないと行を作れないので止める（記録がないときは空の一覧を渡す）
            if (rankingScores == null)
            {
                throw new ArgumentNullException(nameof(rankingScores));
            }
            if (rankingRanks == null)
            {
                throw new ArgumentNullException(nameof(rankingRanks));
            }
            if (rankingScores.Count != rankingRanks.Count)
            {
                throw new ArgumentException($"ランキングのスコア（{rankingScores.Count}件）とランク（{rankingRanks.Count}件）の数がそろっていません", nameof(rankingRanks));
            }

            this.endReason = endReason;
            this.score = score;
            this.correctCount = correctCount;
            this.rankName = rankName;
            this.wrongAnswer = wrongAnswer;
            this.correctAnswer = correctAnswer;
            this.rankingPosition = rankingPosition;

            // 渡された一覧があとで変わっても影響しないように写す
            this.rankingScores = new int[rankingScores.Count];
            this.rankingRanks = new string[rankingRanks.Count];
            for (i = 0; i < rankingScores.Count; i++)
            {
                this.rankingScores[i] = rankingScores[i];
                this.rankingRanks[i] = rankingRanks[i];
            }
        }

        /// <summary>終わった理由</summary>
        public GameEndReason EndReason => endReason;

        /// <summary>スコア（点）</summary>
        public int Score => score;

        /// <summary>正解数（問）</summary>
        public int CorrectCount => correctCount;

        /// <summary>今回のランク（S など）</summary>
        public string RankName => rankName;

        /// <summary>ミスしたときに押した答え（TIME UP では使わない）</summary>
        public GlyphType WrongAnswer => wrongAnswer;

        /// <summary>ミスしたときの正解（TIME UP では使わない）</summary>
        public GlyphType CorrectAnswer => correctAnswer;

        /// <summary>ランキングのスコア（点。高い順）</summary>
        public IReadOnlyList<int> RankingScores => rankingScores;

        /// <summary>ランキングの各スコアのランク（RankingScores と同じ並び）</summary>
        public IReadOnlyList<string> RankingRanks => rankingRanks;

        /// <summary>今回の記録の順位（0 が1位。-1 はランキング外）</summary>
        public int RankingPosition => rankingPosition;

        /// <summary>NEW RECORD か（ランキングの1位に入ったとき）</summary>
        public bool IsNewRecord => rankingPosition == 0;
    }
}
