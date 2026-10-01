using System;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// ランク1つ分の境目（SPEC §1.3）。ランク名と、そのランクになる最低スコアを持つ。
    /// GameBalanceData の一覧に並べて、Inspector で調整する
    /// </summary>
    [Serializable]
    public class RankThreshold
    {
        // ===== 設定（Inspector で調整） =====
        [SerializeField] private string rankName = "D";   // ランク名（S・A・B など）
        [SerializeField, Min(0)] private int minScore;      // このランクになる最低スコア（点）

        /// <summary>
        /// 空の境目を作る（Inspector の一覧で要素を足したときに Unity が使う）
        /// </summary>
        public RankThreshold()
        {
        }

        /// <summary>
        /// ランク名と最低スコアを指定して境目を作る（既定値とテストで使う）
        /// </summary>
        /// <param name="rankName">ランク名</param>
        /// <param name="minScore">このランクになる最低スコア（点）</param>
        public RankThreshold(string rankName, int minScore)
        {
            this.rankName = rankName;
            this.minScore = minScore;
        }

        /// <summary>ランク名（S・A・B など）</summary>
        public string RankName => rankName;

        /// <summary>このランクになる最低スコア（点）</summary>
        public int MinScore => minScore;
    }
}
