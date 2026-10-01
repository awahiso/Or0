using System;
using System.Collections.Generic;

namespace OrZero
{
    /// <summary>
    /// GAME OVER のときに出す煽りメッセージを選ぶ。
    /// 正解の種類とコンボ数で候補を絞り、直前と同じ文言は避けてランダムに1つ選ぶ。
    /// 状態は持たない static 関数だけで作ってあり、EditMode テストで確かめられる
    /// </summary>
    public static class TauntSelector
    {
        // ===== コンボ数の区切り =====
        private const int HighComboThreshold = 15;   // この数以上なら「長く続いたコンボ」向けの文言
        private const int MiddleComboThreshold = 3;  // この数以上なら「増やした時間」向けの文言

        /// <summary>
        /// 煽りメッセージを1つ選ぶ。候補は常に6件で、直前と同じ文言は候補から外す
        /// </summary>
        /// <param name="correctAnswer">正解の種類</param>
        /// <param name="combo">ミスした時点のコンボ数</param>
        /// <param name="previousTaunt">直前に出した文言（なければ null か空文字）</param>
        /// <param name="random">乱数（テストではシードを固定したものを渡す）</param>
        /// <returns>表示する文字</returns>
        public static string ChooseTaunt(GlyphType correctAnswer, int combo, string previousTaunt, Random random)
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> choices;   // 候補の文言
            int index;              // 選んだ候補の添字

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            choices = BuildChoices(correctAnswer, combo);

            // 直前と同じ文言を外す（候補は6件あるので、0件にはならない）
            if (!string.IsNullOrEmpty(previousTaunt))
            {
                choices.RemoveAll(choice => choice == previousTaunt);
            }

            index = random.Next(choices.Count);
            return choices[index];
        }

        /// <summary>
        /// 状況に応じた候補のリストを組み立てる（正解の種類で2件・コンボ数で2件・共通で2件）
        /// </summary>
        /// <param name="correctAnswer">正解の種類</param>
        /// <param name="combo">ミスした時点のコンボ数</param>
        /// <returns>候補の文言（6件）</returns>
        public static List<string> BuildChoices(GlyphType correctAnswer, int combo)
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> choices = new List<string>(6);   // 候補の文言

            // 正解の種類に応じた文言
            if (correctAnswer == GlyphType.LetterO)
            {
                choices.Add("英字の「O」です。数字にしか見えませんでした？");
                choices.Add("アルファベットでした。0点みたいな選択でしたね。");
            }
            else
            {
                choices.Add("数字の「0」です。丸なら全部「O」に見えますか？");
                choices.Add("ゼロでした。英字っぽく見えた、ということにします？");
            }

            // コンボ数に応じた文言
            if (combo >= HighComboThreshold)
            {
                choices.Add("積み上げたコンボ、今ので全部ゼロです。");
                choices.Add("そこまで続いたのに、最後だけ自信満々で不正解です。");
            }
            else if (combo >= MiddleComboThreshold)
            {
                choices.Add("せっかく増やした時間、使い切る前に終了です。");
                choices.Add("コンボより先に、集中力が途切れましたね。");
            }
            else
            {
                choices.Add("開始早々、それですか。");
                choices.Add("その自信だけは正解でした。");
            }

            // 共通の文言
            choices.Add("一瞬の迷いではなく、一瞬の即決ミスでした。");
            choices.Add("よく見れば分かりました。よく見れば、ですけど。");

            return choices;
        }
    }
}