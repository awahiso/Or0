using System;
using System.Collections.Generic;

namespace OrZero
{
    /// <summary>
    /// スコアからランク（S〜D など）を決めるクラス（SPEC §1.3）。
    /// 最低スコアに届いているランクのうち、最低スコアがいちばん高いものを選ぶ（一覧の並び順には左右されない）。
    /// どのランクにも届かないときは、最低スコアがいちばん低いランクにする。
    /// Unity に依存しない純粋な計算なので、EditMode テストで確かめられる
    /// </summary>
    public static class RankEvaluator
    {
        /// <summary>
        /// スコアからランク名を決める
        /// </summary>
        /// <param name="score">スコア（点）</param>
        /// <param name="thresholds">ランクの境目（1つ以上。並び順は自由）</param>
        /// <returns>ランク名</returns>
        public static string Evaluate(int score, IReadOnlyList<RankThreshold> thresholds)
        {
            // ローカル変数は関数の先頭で宣言する
            RankThreshold reached;   // 届いているランクのうち、最低スコアがいちばん高いもの（まだなければ null）
            RankThreshold lowest;    // 最低スコアがいちばん低いランク（どれにも届かないときに使う）
            int i;                   // ループ用の添字

            // 境目がないとランクを決められないので止める（GameBalanceData の設定ミス）
            if (thresholds == null)
            {
                throw new ArgumentNullException(nameof(thresholds));
            }
            if (thresholds.Count == 0)
            {
                throw new ArgumentException("ランクの境目が1つもありません", nameof(thresholds));
            }

            reached = null;
            lowest = null;
            for (i = 0; i < thresholds.Count; i++)
            {
                if (thresholds[i] == null)
                {
                    throw new ArgumentException($"ランクの境目の {i} 番目が空です", nameof(thresholds));
                }

                // どれにも届かないときのために、いちばん低いランクを覚えておく
                if (lowest == null || thresholds[i].MinScore < lowest.MinScore)
                {
                    lowest = thresholds[i];
                }

                // 届いているランクの中で、より高いものに置き換える（境目ちょうどは届いている扱い）
                if (thresholds[i].MinScore <= score && (reached == null || thresholds[i].MinScore > reached.MinScore))
                {
                    reached = thresholds[i];
                }
            }

            return reached != null ? reached.RankName : lowest.RankName;
        }
    }
}
