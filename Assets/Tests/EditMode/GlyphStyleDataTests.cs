using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OrZero.Tests
{
    /// <summary>
    /// GlyphStyleData（出題スタイル）のテスト（EditMode）
    /// SPEC §1.4 の「見分けにくさの判定（縦横比の差 0.05 未満）」と「表示する文字の入力チェック」を確かめる
    /// </summary>
    public class GlyphStyleDataTests
    {
        // ===== テストで共通に使う値 =====
        private const float Tolerance = 0.0001f;   // float の比較で許す誤差

        /// <summary>
        /// 丸い O と縦長の 0 なら、縦横比の差はプラスになる（Arial 互換の書体に近い値で確かめる）
        /// </summary>
        [Test]
        public void CalculateAspectDifference_RoundOAndNarrowZero_ReturnsPositive()
        {
            // ローカル変数は関数の先頭で宣言する
            float difference;   // 縦横比の差（英字側 − 数字側）

            // 実行: O は 幅0.967×高さ1、0 は 幅0.675×高さ1
            difference = GlyphStyleData.CalculateAspectDifference(0.967f, 1f, 0.675f, 1f);

            // 確認
            Assert.AreEqual(0.292f, difference, Tolerance);
        }

        /// <summary>
        /// 同じ形なら差は 0（大きさが違っても縦横比が同じなら 0）
        /// </summary>
        [Test]
        public void CalculateAspectDifference_SameShapeDifferentSize_ReturnsZero()
        {
            // ローカル変数は関数の先頭で宣言する
            float difference;   // 縦横比の差

            // 実行: 幅40×高さ50 と 幅80×高さ100（どちらも縦横比 0.8）
            difference = GlyphStyleData.CalculateAspectDifference(40f, 50f, 80f, 100f);

            // 確認
            Assert.AreEqual(0f, difference, Tolerance);
        }

        /// <summary>
        /// 高さが 0 以下の文字は縦横比を計算できないので、例外になる
        /// </summary>
        [Test]
        public void CalculateAspectDifference_ZeroHeight_ThrowsArgumentOutOfRangeException()
        {
            // 実行・確認
            Assert.Throws<ArgumentOutOfRangeException>(() => GlyphStyleData.CalculateAspectDifference(1f, 0f, 1f, 1f));
        }

        /// <summary>
        /// 差の大きさが 0.05 未満なら「見分けにくい」。向き（プラス・マイナス）は関係ない
        /// </summary>
        [Test]
        public void IsHardToDistinguish_AroundThreshold_JudgesByAbsoluteValue()
        {
            // 実行・確認
            Assert.IsTrue(GlyphStyleData.IsHardToDistinguish(0.049f));    // 境目のすぐ下 → 見分けにくい
            Assert.IsFalse(GlyphStyleData.IsHardToDistinguish(0.05f));    // 境目ちょうど → 使える
            Assert.IsTrue(GlyphStyleData.IsHardToDistinguish(-0.03f));    // マイナスでも小さければ見分けにくい
            Assert.IsFalse(GlyphStyleData.IsHardToDistinguish(-0.2f));    // 逆向きでも差が大きければ見分けられる
        }

        /// <summary>
        /// 既定の「O」と「0」は問題なし
        /// </summary>
        [Test]
        public void FindTextProblems_DefaultTexts_NoProblem()
        {
            // 実行・確認
            Assert.AreEqual(0, GlyphStyleData.FindTextProblems("O", "0").Count);
        }

        /// <summary>
        /// 全角の「Ｏ」「０」や小文字の「o」も、英字側・数字側として正しい
        /// </summary>
        [Test]
        public void FindTextProblems_FullWidthAndLowercase_NoProblem()
        {
            // 実行・確認
            Assert.AreEqual(0, GlyphStyleData.FindTextProblems("Ｏ", "０").Count);
            Assert.AreEqual(0, GlyphStyleData.FindTextProblems("o", "0").Count);
        }

        /// <summary>
        /// 英字側に数字、数字側に英字を入れると、両方とも問題として挙がる
        /// </summary>
        [Test]
        public void FindTextProblems_SwappedTexts_ReportsBothSides()
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> problems;   // 見つかった問題

            // 実行
            problems = GlyphStyleData.FindTextProblems("0", "O");

            // 確認: 英字側・数字側の2つ
            Assert.AreEqual(2, problems.Count);
        }

        /// <summary>
        /// 空の文字や、2文字以上の文字は問題として挙がる
        /// </summary>
        [Test]
        public void FindTextProblems_EmptyOrTooLong_ReportsProblem()
        {
            // 実行・確認
            Assert.AreEqual(1, GlyphStyleData.FindTextProblems("", "0").Count);
            Assert.AreEqual(1, GlyphStyleData.FindTextProblems("O", "00").Count);
        }

        /// <summary>
        /// 答えの種類に応じて、表示する文字を返す（既定は O と 0）
        /// </summary>
        [Test]
        public void GetText_DefaultStyle_ReturnsTextForEachAnswer()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData style;   // テスト対象

            style = ScriptableObject.CreateInstance<GlyphStyleData>();
            try
            {
                // 実行・確認
                Assert.AreEqual("O", style.GetText(GlyphType.LetterO));
                Assert.AreEqual("0", style.GetText(GlyphType.DigitZero));
            }
            finally
            {
                // テストで作ったアセットは必ず消す
                UnityEngine.Object.DestroyImmediate(style);
            }
        }
    }
}
