using System;

namespace OrZero
{
    /// <summary>
    /// 1問の獲得点を計算するクラス（SPEC §1.3）。
    /// 獲得点 = 基本点 + スピード加点。スピード加点は速く答えるほど大きく、猶予を過ぎると 0。
    /// Unity に依存しない純粋な計算なので、EditMode テストで確かめられる
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// 1問の獲得点を計算する
        /// </summary>
        /// <param name="basePoint">基本点（点）</param>
        /// <param name="speedBonusMax">スピード加点の最大（点）。0秒で答えたときの加点</param>
        /// <param name="speedBonusWindowSeconds">スピード加点がもらえる猶予（秒。0 より大きい）</param>
        /// <param name="answerSeconds">文字が表示されてから答えるまでの秒数</param>
        /// <returns>獲得点（点）</returns>
        public static int CalculatePoint(int basePoint, int speedBonusMax, float speedBonusWindowSeconds, float answerSeconds)
        {
            // ローカル変数は関数の先頭で宣言する
            double speedRate;   // 猶予に対してどれだけ速く答えたか（0〜1。1 が最速）
            int speedBonus;     // スピード加点（点）

            // 猶予が 0 以下だと割り算ができないので、設定ミスとして止める
            if (speedBonusWindowSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speedBonusWindowSeconds), speedBonusWindowSeconds, "スピード加点の猶予は0より大きくしてください");
            }

            // 速さの割合を 0〜1 に収める（猶予を過ぎたら 0、マイナスの回答時間でも 1 を超えない）
            speedRate = 1.0 - answerSeconds / (double)speedBonusWindowSeconds;
            speedRate = Math.Max(0.0, Math.Min(1.0, speedRate));

            // スピード加点は整数に四捨五入する
            // （切り捨てると、0.8秒 → 19.999… → 19点のように、浮動小数点の誤差で1点ずれるため。SPEC §1.3）
            speedBonus = (int)Math.Round(speedBonusMax * speedRate, MidpointRounding.AwayFromZero);

            return basePoint + speedBonus;
        }
    }
}
