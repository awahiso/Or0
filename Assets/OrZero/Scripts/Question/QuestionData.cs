using System;

namespace OrZero
{
    /// <summary>
    /// 1問分の出題内容（答え・スタイル・大きさ・装飾の乱数）。
    /// QuestionGenerator が作り、GlyphView（T9）と GlyphDecorationView（T10）が表示に使う。
    /// 作ったあとは中身を変えない
    /// </summary>
    public class QuestionData
    {
        // ===== 出題内容（作るときに決まり、あとから変えない） =====
        private readonly GlyphType answer;         // 答え（英字側／数字側）
        private readonly GlyphStyleData style;     // 見た目のスタイル
        private readonly float scale;              // 大きさの倍率（縦横同じ比率）
        private readonly int decorationSeed;       // 装飾の配置に使う乱数のシード（答えとは無関係に抽選）

        /// <summary>
        /// 1問分の出題内容を作る
        /// </summary>
        /// <param name="answer">答え</param>
        /// <param name="style">見た目のスタイル（null 不可）</param>
        /// <param name="scale">大きさの倍率</param>
        /// <param name="decorationSeed">装飾の配置に使う乱数のシード</param>
        public QuestionData(GlyphType answer, GlyphStyleData style, float scale, int decorationSeed)
        {
            // スタイルがないと表示できないので止める
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            this.answer = answer;
            this.style = style;
            this.scale = scale;
            this.decorationSeed = decorationSeed;
        }

        /// <summary>答え（英字側／数字側）</summary>
        public GlyphType Answer => answer;

        /// <summary>見た目のスタイル</summary>
        public GlyphStyleData Style => style;

        /// <summary>大きさの倍率（縦横同じ比率）</summary>
        public float Scale => scale;

        /// <summary>装飾の配置に使う乱数のシード</summary>
        public int DecorationSeed => decorationSeed;

        /// <summary>表示する文字（スタイルの設定に従う。既定は O か 0）</summary>
        public string Text => style.GetText(answer);
    }
}
