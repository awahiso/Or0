using System.Collections.Generic;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// ゲーム全体の調整値をまとめた ScriptableObject（SPEC §4.1）。
    /// 値は Inspector で調整する。実行中はプログラムから書き換えない（エディタではアセットに保存されてしまうため）。
    /// 項目はタスクごとに必要になったものを足していく
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalance", menuName = "OrZero/Game Balance")]
    public class GameBalanceData : ScriptableObject
    {
        // ===== 出題 =====
        [SerializeField, Min(1)] private int maxSameAnswerStreak = 4;   // 同じ答えを続けてよい最大回数（回）。SPEC §1.4 ルール5
        [SerializeField, Min(0f)] private float inputLockSeconds = 0.08f;   // 文字が切り替わった直後に入力を受け付けない時間（秒）。SPEC §1.4 ルール6

        // ===== 制限時間 =====
        [SerializeField, Min(0.01f)] private float startSeconds = 60f;    // 開始時の残り時間（秒）
        [SerializeField, Min(0.01f)] private float maxSeconds = 99.99f;   // 残り時間の上限（秒）。表示を整数2桁に収めるため
        [SerializeField, Min(1)] private int extendInterval = 3;          // 何問正解するごとに延長するか（問）
        [SerializeField, Min(0f)] private float extendSeconds = 1.0f;     // 1回の延長で増やす秒数（秒）

        // ===== 得点（SPEC §1.3） =====
        [SerializeField, Min(0)] private int basePoint = 100;                        // 1問正解の基本点（点）
        [SerializeField, Min(0)] private int speedBonusMax = 100;                    // スピード加点の最大（点）。0秒で答えたときの加点
        [SerializeField, Min(0.01f)] private float speedBonusWindowSeconds = 1.0f;   // スピード加点がもらえる猶予（秒）。これより遅いと加点なし

        // ===== ランク（SPEC §1.3） =====
        // ランクの境目（ランク名と、そのランクになる最低スコア）。並び順は自由。いちばん下のランクは最低スコア 0 にしておく
        [SerializeField] private RankThreshold[] rankThresholds =
        {
            new RankThreshold("S", 10000),
            new RankThreshold("A", 6500),
            new RankThreshold("B", 3500),
            new RankThreshold("C", 1500),
            new RankThreshold("D", 0),
        };

        // ===== ランキング（SPEC のランキングの節） =====
        [SerializeField, Min(1)] private int rankingSize = 5;   // ランキングに残す件数（件）

        /// <summary>同じ答えを続けてよい最大回数（回）</summary>
        public int MaxSameAnswerStreak => maxSameAnswerStreak;

        /// <summary>文字が切り替わった直後に入力を受け付けない時間（秒）</summary>
        public float InputLockSeconds => inputLockSeconds;

        /// <summary>開始時の残り時間（秒）</summary>
        public float StartSeconds => startSeconds;

        /// <summary>残り時間の上限（秒）</summary>
        public float MaxSeconds => maxSeconds;

        /// <summary>何問正解するごとに延長するか（問）</summary>
        public int ExtendInterval => extendInterval;

        /// <summary>1回の延長で増やす秒数（秒）</summary>
        public float ExtendSeconds => extendSeconds;

        /// <summary>1問正解の基本点（点）</summary>
        public int BasePoint => basePoint;

        /// <summary>スピード加点の最大（点）</summary>
        public int SpeedBonusMax => speedBonusMax;

        /// <summary>スピード加点がもらえる猶予（秒）</summary>
        public float SpeedBonusWindowSeconds => speedBonusWindowSeconds;

        /// <summary>ランクの境目（ランク名と最低スコア）</summary>
        public IReadOnlyList<RankThreshold> RankThresholds => rankThresholds;

        /// <summary>ランキングに残す件数（件）</summary>
        public int RankingSize => rankingSize;

        /// <summary>
        /// Inspector で値を変えたときに、組み合わせのおかしな設定を警告する（エディタでのみ呼ばれる）
        /// </summary>
        private void OnValidate()
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字
            int j;   // 比べる相手の添字

            if (startSeconds > maxSeconds)
            {
                Debug.LogWarning($"GameBalanceData: 開始時の残り時間（{startSeconds}秒）が上限（{maxSeconds}秒）を超えています", this);
            }

            // ランクの境目: 1つもない・名前が空・最低スコアが同じ（片方のランクに絶対ならない）を警告する
            if (rankThresholds == null || rankThresholds.Length == 0)
            {
                Debug.LogWarning("GameBalanceData: ランクの境目が1つもありません（S〜D などを設定してください）", this);
                return;
            }
            for (i = 0; i < rankThresholds.Length; i++)
            {
                if (rankThresholds[i] == null || string.IsNullOrEmpty(rankThresholds[i].RankName))
                {
                    Debug.LogWarning($"GameBalanceData: ランクの境目の {i} 番目に、ランク名が入っていません", this);
                    continue;
                }
                for (j = i + 1; j < rankThresholds.Length; j++)
                {
                    if (rankThresholds[j] != null && rankThresholds[j].MinScore == rankThresholds[i].MinScore)
                    {
                        Debug.LogWarning($"GameBalanceData: ランク「{rankThresholds[i].RankName}」と「{rankThresholds[j].RankName}」の最低スコアが同じ（{rankThresholds[i].MinScore}点）なので、片方には絶対になりません", this);
                    }
                }
            }
        }
    }
}
