using System;
using System.Collections.Generic;

namespace OrZero
{
    /// <summary>
    /// 1問分の出題内容を作るクラス（SPEC §1.4）。
    /// 答え（英字／数字）は AnswerPicker で、スタイルは出やすさの重みつきで、それぞれ独立に抽選する
    /// （公平性ルール1: スタイルから答えが分からないようにするため）。
    /// スタイルは、今の正解数で解禁されているものだけから選ぶ（正解を重ねるほど出てくる書体が増える。T22）。
    /// Unity に依存しない純粋なロジックなので、EditMode テストで確かめられる
    /// </summary>
    public class QuestionGenerator
    {
        // ===== 抽選に使うもの（作るときに決まり、あとから変えない） =====
        private readonly AnswerPicker answerPicker;     // 答えの抽選（同じ答えの連続上限つき。初期化済みのもの）
        private readonly List<GlyphStyleData> styles;   // 抽選に使うスタイルの一覧
        private readonly float[] styleWeights;          // 各スタイルの出やすさ（styles と同じ並び）
        private readonly int[] styleUnlockCounts;       // 各スタイルの解禁する正解数（styles と同じ並び）。出やすさと同じく、作ったときの値で固定する
        private readonly Random random;                 // スタイル・大きさ・装飾の抽選に使う乱数

        // ===== 抽選の作業用（毎問 new しないように使い回す） =====
        private readonly float[] availableWeights;      // 今の正解数で選べるスタイルの出やすさ（まだ解禁されていないものは 0）

        /// <summary>
        /// 出題の準備をする
        /// </summary>
        /// <param name="answerPicker">答えの抽選（Initialize 済みのもの）</param>
        /// <param name="styles">使うスタイル（1つ以上。1問目から出るスタイルの出やすさの合計が 0 より大きいこと）</param>
        /// <param name="random">スタイル・大きさ・装飾の抽選に使う乱数（テストではシードを固定したものを渡す）</param>
        public QuestionGenerator(AnswerPicker answerPicker, IReadOnlyList<GlyphStyleData> styles, Random random)
        {
            // ローカル変数は関数の先頭で宣言する
            float totalWeight;   // 出やすさの合計
            float startWeight;   // 1問目（正解数 0）から出せるスタイルの出やすさの合計
            int i;               // ループ用の添字

            if (answerPicker == null)
            {
                throw new ArgumentNullException(nameof(answerPicker));
            }
            if (styles == null)
            {
                throw new ArgumentNullException(nameof(styles));
            }
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            // スタイルが1つもないと出題できない
            if (styles.Count == 0)
            {
                throw new ArgumentException("スタイルが1つもありません（Resources/GlyphStyles にスタイルを置いてください）", nameof(styles));
            }

            this.answerPicker = answerPicker;
            this.styles = new List<GlyphStyleData>(styles);   // 渡された一覧があとで変わっても影響しないように写す
            this.random = random;
            styleWeights = new float[this.styles.Count];
            styleUnlockCounts = new int[this.styles.Count];
            availableWeights = new float[this.styles.Count];

            totalWeight = 0f;
            startWeight = 0f;
            for (i = 0; i < this.styles.Count; i++)
            {
                if (this.styles[i] == null)
                {
                    throw new ArgumentException($"スタイルの {i} 番目が空です", nameof(styles));
                }
                styleWeights[i] = Math.Max(0f, this.styles[i].Weight);
                styleUnlockCounts[i] = this.styles[i].UnlockCorrectCount;
                totalWeight += styleWeights[i];
                if (IsUnlocked(styleUnlockCounts[i], 0))
                {
                    startWeight += styleWeights[i];
                }
            }

            // 出やすさがすべて 0 だと、どのスタイルも選べない
            if (totalWeight <= 0f)
            {
                throw new ArgumentException("すべてのスタイルの出やすさが 0 です", nameof(styles));
            }

            // 1問目で選べるスタイルがないと出題できない。
            // 解禁は正解を重ねるほど増える一方なので、ここを満たせば途中で選べるものがなくなることはない
            if (startWeight <= 0f)
            {
                throw new ArgumentException("1問目から出せるスタイルがありません（解禁する正解数が 0 で、出やすさが 0 より大きいスタイルを1つ以上置いてください）", nameof(styles));
            }
        }

        /// <summary>
        /// 1問分の出題内容を作る（答え・スタイル・大きさ・装飾の乱数）。
        /// スタイルは、今の正解数で解禁されているものだけから選ぶ
        /// </summary>
        /// <param name="correctCount">これまでの正解数（0 以上。1問目は 0）</param>
        /// <returns>出題内容</returns>
        public QuestionData Generate(int correctCount)
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphType answer;        // 答え
            GlyphStyleData style;    // 見た目のスタイル
            float smallest;          // 大きさの下限
            float largest;           // 大きさの上限
            float scale;             // 大きさの倍率
            int decorationSeed;      // 装飾の配置に使う乱数のシード
            int i;                   // ループ用の添字

            // 正解数がマイナスになることはない（呼び出し側のミス）ので止める
            if (correctCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(correctCount), correctCount, "正解数は0以上にしてください");
            }

            // まだ解禁されていないスタイルは、出やすさを 0 にして選ばれないようにする
            for (i = 0; i < styles.Count; i++)
            {
                availableWeights[i] = IsUnlocked(styleUnlockCounts[i], correctCount) ? styleWeights[i] : 0f;
            }

            // 答えとスタイルは別々に抽選する（どちらかを見て、もう一方を決めない）
            answer = answerPicker.PickNext();
            style = styles[PickWeightedIndex(availableWeights, random.NextDouble())];

            // 大きさは最小〜最大の間でばらつかせる（設定が逆でも範囲の外に出ないようにする）
            smallest = Math.Min(style.MinScale, style.MaxScale);
            largest = Math.Max(style.MinScale, style.MaxScale);
            scale = smallest + (float)random.NextDouble() * (largest - smallest);

            decorationSeed = random.Next();
            return new QuestionData(answer, style, scale, decorationSeed);
        }

        /// <summary>
        /// スタイルが解禁されているか（今の正解数が、解禁する正解数に届いているか）
        /// </summary>
        /// <param name="unlockCorrectCount">スタイルの解禁する正解数</param>
        /// <param name="correctCount">今の正解数</param>
        /// <returns>解禁されていれば true</returns>
        private static bool IsUnlocked(int unlockCorrectCount, int correctCount)
        {
            return correctCount >= unlockCorrectCount;
        }

        /// <summary>
        /// 重みつきで番号を選ぶ（0〜1 の乱数を、重みの割合で区切った区間に当てはめる）。
        /// 重みが 0 以下のものは選ばない
        /// </summary>
        /// <param name="weights">それぞれの重み（1つ以上は 0 より大きいこと）</param>
        /// <param name="roll">0 以上 1 未満の乱数</param>
        /// <returns>選ばれた番号</returns>
        public static int PickWeightedIndex(IReadOnlyList<float> weights, double roll)
        {
            // ローカル変数は関数の先頭で宣言する
            double total;        // 重みの合計
            double target;       // 乱数を重みの合計に合わせた値
            double cumulative;   // ここまでの重みの累計
            int lastPositive;    // 最後に見つかった、重みが 0 より大きい番号
            int i;               // ループ用の添字

            total = 0.0;
            for (i = 0; i < weights.Count; i++)
            {
                total += Math.Max(0f, weights[i]);
            }
            if (total <= 0.0)
            {
                throw new ArgumentException("重みの合計が 0 です", nameof(weights));
            }

            // 乱数を 0〜1 に収めてから、重みの合計に合わせる
            target = Math.Max(0.0, Math.Min(roll, 1.0)) * total;
            cumulative = 0.0;
            lastPositive = -1;
            for (i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }
                lastPositive = i;
                cumulative += weights[i];
                if (target < cumulative)
                {
                    return i;
                }
            }

            // 小数の誤差で最後まで届かなかったときは、最後の選べる番号にする
            return lastPositive;
        }
    }
}
