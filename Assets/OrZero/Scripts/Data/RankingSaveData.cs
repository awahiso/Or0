using System;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// ランキングを保存するときの入れ物（JsonUtility は配列だけを直接 JSON にできないので、クラスに包む）。
    /// 保存の形は {"scores":[...]}。項目名を変えると、今までの保存データが読めなくなるので変えない
    /// </summary>
    [Serializable]
    public class RankingSaveData
    {
        // ===== 保存する中身 =====
        [SerializeField] private int[] scores = new int[0];   // スコア（点。高い順）

        /// <summary>
        /// 空の入れ物を作る（JsonUtility が JSON から戻すときに使う）
        /// </summary>
        public RankingSaveData()
        {
        }

        /// <summary>
        /// スコアを指定して入れ物を作る（保存するときに使う）
        /// </summary>
        /// <param name="scores">スコア（高い順）</param>
        public RankingSaveData(int[] scores)
        {
            this.scores = scores;
        }

        /// <summary>スコア（点。高い順）</summary>
        public int[] Scores => scores;
    }
}
