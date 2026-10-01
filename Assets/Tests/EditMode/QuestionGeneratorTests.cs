using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OrZero.Tests
{
    /// <summary>
    /// QuestionGenerator（答えとスタイルの抽選）のテスト（EditMode）
    /// SPEC §1.4 公平性ルール1「答えの抽選とスタイルの抽選は独立」と、出やすさの重み、
    /// 正解数によるスタイルの解禁（T22）を確かめる。解禁を扱わないテストは、正解数 0（1問目）で抽選する
    /// </summary>
    public class QuestionGeneratorTests
    {
        // ===== テストで共通に使う値 =====
        private const int DrawCount = 20000;   // 統計を見るときの抽選回数（回）
        private const int StreakLimit = 4;     // 同じ答えの連続上限（回）

        // ===== テストで作ったスタイル（最後に消す） =====
        private readonly List<GlyphStyleData> createdStyles = new List<GlyphStyleData>();

        /// <summary>
        /// テストごとに、作ったスタイルを消す
        /// </summary>
        [TearDown]
        public void DestroyCreatedStyles()
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字

            for (i = 0; i < createdStyles.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(createdStyles[i]);
            }
            createdStyles.Clear();
        }

        /// <summary>
        /// 2つのスタイルそれぞれで、英字と数字がおおむね半々で出る（スタイルから答えが分からない）
        /// </summary>
        [Test]
        public void Generate_TwoStyles_EachStyleShowsBothAnswersAboutHalf()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData styleA;         // スタイルA
            GlyphStyleData styleB;         // スタイルB
            QuestionGenerator generator;   // テスト対象
            QuestionData question;         // 1問分の出題
            int countA;                    // スタイルAが出た回数
            int letterA;                   // スタイルAで英字が出た回数
            int countB;                    // スタイルBが出た回数
            int letterB;                   // スタイルBで英字が出た回数
            int i;                         // ループ用の添字

            // 準備: 出やすさが同じスタイルを2つ
            styleA = CreateStyle(1f, 1f, 1f);
            styleB = CreateStyle(1f, 1f, 1f);
            generator = CreateGenerator(new List<GlyphStyleData> { styleA, styleB }, 100);

            // 実行
            countA = 0; letterA = 0; countB = 0; letterB = 0;
            for (i = 0; i < DrawCount; i++)
            {
                question = generator.Generate(0);
                if (question.Style == styleA)
                {
                    countA++;
                    letterA += question.Answer == GlyphType.LetterO ? 1 : 0;
                }
                else
                {
                    countB++;
                    letterB += question.Answer == GlyphType.LetterO ? 1 : 0;
                }
            }

            // 確認: どちらのスタイルでも英字の割合が 47〜53%
            Assert.That((double)letterA / countA, Is.InRange(0.47, 0.53));
            Assert.That((double)letterB / countB, Is.InRange(0.47, 0.53));
        }

        /// <summary>
        /// 出やすさ 2:1 なら、スタイルはおおむね 2:1 の割合で選ばれる
        /// </summary>
        [Test]
        public void Generate_WeightsTwoToOne_PicksProportionally()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData heavy;          // 出やすさ 2 のスタイル
            GlyphStyleData light;          // 出やすさ 1 のスタイル
            QuestionGenerator generator;   // テスト対象
            int heavyCount;                // 出やすさ 2 のスタイルが出た回数
            int i;                         // ループ用の添字

            // 準備
            heavy = CreateStyle(2f, 1f, 1f);
            light = CreateStyle(1f, 1f, 1f);
            generator = CreateGenerator(new List<GlyphStyleData> { heavy, light }, 200);

            // 実行
            heavyCount = 0;
            for (i = 0; i < DrawCount; i++)
            {
                heavyCount += generator.Generate(0).Style == heavy ? 1 : 0;
            }

            // 確認: 2/3（約0.667）の前後
            Assert.That((double)heavyCount / DrawCount, Is.InRange(0.64, 0.69));
        }

        /// <summary>
        /// スタイルが1つだけでも動き、英字と数字の両方が出る
        /// </summary>
        [Test]
        public void Generate_SingleStyle_ShowsBothAnswers()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData style;          // ただ1つのスタイル
            QuestionGenerator generator;   // テスト対象
            QuestionData question;         // 1問分の出題
            int letterCount;               // 英字が出た回数
            int i;                         // ループ用の添字

            // 準備
            style = CreateStyle(1f, 1f, 1f);
            generator = CreateGenerator(new List<GlyphStyleData> { style }, 300);

            // 実行
            letterCount = 0;
            for (i = 0; i < 100; i++)
            {
                question = generator.Generate(0);
                Assert.AreEqual(style, question.Style);
                letterCount += question.Answer == GlyphType.LetterO ? 1 : 0;
            }

            // 確認: 100問のうち、英字も数字も出ている
            Assert.That(letterCount, Is.InRange(1, 99));
        }

        /// <summary>
        /// 大きさはスタイルの最小〜最大の範囲に収まり、毎回同じではない
        /// </summary>
        [Test]
        public void Generate_ScaleRange_StaysWithinMinMax()
        {
            // ローカル変数は関数の先頭で宣言する
            QuestionGenerator generator;   // テスト対象
            float scale;                   // 1問分の大きさ
            float smallest;                // これまでの最小
            float largest;                 // これまでの最大
            int i;                         // ループ用の添字

            // 準備: 0.8〜1.2倍のスタイル
            generator = CreateGenerator(new List<GlyphStyleData> { CreateStyle(1f, 0.8f, 1.2f) }, 400);

            // 実行
            smallest = float.MaxValue;
            largest = float.MinValue;
            for (i = 0; i < 1000; i++)
            {
                scale = generator.Generate(0).Scale;
                smallest = Math.Min(smallest, scale);
                largest = Math.Max(largest, scale);
            }

            // 確認: 範囲の中に収まり、ばらついている
            Assert.That(smallest, Is.InRange(0.8, 1.2));
            Assert.That(largest, Is.InRange(0.8, 1.2));
            Assert.Greater(largest - smallest, 0.3f);
        }

        /// <summary>
        /// 表示する文字はスタイルの設定に従う（全角のスタイルなら全角の文字が出る）
        /// </summary>
        [Test]
        public void Generate_FullWidthStyle_TextFollowsAnswer()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData style;          // 全角のスタイル
            QuestionGenerator generator;   // テスト対象
            QuestionData question;         // 1問分の出題
            int i;                         // ループ用の添字

            // 準備: 英字側「Ｏ」、数字側「０」
            style = CreateStyle(1f, 1f, 1f);
            SetString(style, "letterText", "Ｏ");
            SetString(style, "digitText", "０");
            generator = CreateGenerator(new List<GlyphStyleData> { style }, 500);

            // 実行・確認
            for (i = 0; i < 20; i++)
            {
                question = generator.Generate(0);
                Assert.AreEqual(question.Answer == GlyphType.LetterO ? "Ｏ" : "０", question.Text);
            }
        }

        /// <summary>
        /// スタイルが1つもないと、出題できないので例外になる
        /// </summary>
        [Test]
        public void Constructor_NoStyles_ThrowsArgumentException()
        {
            // 実行・確認
            Assert.Throws<ArgumentException>(() => CreateGenerator(new List<GlyphStyleData>(), 600));
        }

        /// <summary>
        /// 出やすさがすべて 0 だと、どのスタイルも選べないので例外になる
        /// </summary>
        [Test]
        public void Constructor_AllWeightsZero_ThrowsArgumentException()
        {
            // 実行・確認
            Assert.Throws<ArgumentException>(() => CreateGenerator(new List<GlyphStyleData> { CreateStyle(0f, 1f, 1f) }, 700));
        }

        /// <summary>
        /// 重みつきの抽選の境目（0〜1 の乱数を、重みの割合で区切る。重み 0 のものは選ばない）
        /// </summary>
        [Test]
        public void PickWeightedIndex_Boundaries_PicksExpectedIndex()
        {
            // 実行・確認: 重み 1:1 なら 0.5 未満が 0 番、0.5 以上が 1 番
            Assert.AreEqual(0, QuestionGenerator.PickWeightedIndex(new[] { 1f, 1f }, 0.0));
            Assert.AreEqual(0, QuestionGenerator.PickWeightedIndex(new[] { 1f, 1f }, 0.4999));
            Assert.AreEqual(1, QuestionGenerator.PickWeightedIndex(new[] { 1f, 1f }, 0.5));
            Assert.AreEqual(1, QuestionGenerator.PickWeightedIndex(new[] { 1f, 1f }, 0.9999));
            // 重み 0 のものは、乱数がいくつでも選ばれない
            Assert.AreEqual(1, QuestionGenerator.PickWeightedIndex(new[] { 0f, 1f }, 0.0));
        }

        /// <summary>
        /// 解禁する正解数の手前（0〜4問正解）では、そのスタイルは1回も選ばれない
        /// </summary>
        [Test]
        public void Generate_BeforeUnlockCount_NeverPicksLockedStyle()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData early;          // 1問目から出るスタイル
            GlyphStyleData late;           // 5問正解で解禁されるスタイル
            QuestionGenerator generator;   // テスト対象
            int lateCount;                 // 解禁前に late が出た回数（0 のはず）
            int correctCount;              // 抽選するときの正解数
            int i;                         // ループ用の添字

            // 準備: 出やすさは同じで、late だけ 5問正解で解禁
            early = CreateStyle(1f, 1f, 1f);
            late = CreateStyle(1f, 1f, 1f);
            SetInt(late, "unlockCorrectCount", 5);
            generator = CreateGenerator(new List<GlyphStyleData> { early, late }, 800);

            // 実行: 正解数 0〜4 で、それぞれ 2000 回ずつ抽選する
            lateCount = 0;
            for (correctCount = 0; correctCount < 5; correctCount++)
            {
                for (i = 0; i < 2000; i++)
                {
                    lateCount += generator.Generate(correctCount).Style == late ? 1 : 0;
                }
            }

            // 確認
            Assert.AreEqual(0, lateCount);
        }

        /// <summary>
        /// 解禁する正解数ちょうどで出始め、そこからは出やすさどおりに選ばれる（1:1 ならおおむね半々）
        /// </summary>
        [Test]
        public void Generate_ReachedUnlockCount_PicksByWeight()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData early;          // 1問目から出るスタイル
            GlyphStyleData late;           // 5問正解で解禁されるスタイル
            QuestionGenerator generator;   // テスト対象
            int lateCount;                 // late が出た回数
            int i;                         // ループ用の添字

            // 準備: 出やすさは同じで、late だけ 5問正解で解禁
            early = CreateStyle(1f, 1f, 1f);
            late = CreateStyle(1f, 1f, 1f);
            SetInt(late, "unlockCorrectCount", 5);
            generator = CreateGenerator(new List<GlyphStyleData> { early, late }, 900);

            // 実行: ちょうど解禁される正解数 5 で抽選する
            lateCount = 0;
            for (i = 0; i < DrawCount; i++)
            {
                lateCount += generator.Generate(5).Style == late ? 1 : 0;
            }

            // 確認: 1/2 の前後（47〜53%）
            Assert.That((double)lateCount / DrawCount, Is.InRange(0.47, 0.53));
        }

        /// <summary>
        /// 解禁する正解数が 0・3・6 のスタイルなら、出てくる種類は 1 → 2 → 3 と段階的に増える
        /// </summary>
        [Test]
        public void Generate_DifferentUnlockCounts_KindsIncreaseStepByStep()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData first;                // 1問目から出るスタイル
            GlyphStyleData second;               // 3問正解で解禁されるスタイル
            GlyphStyleData third;                // 6問正解で解禁されるスタイル
            QuestionGenerator generator;         // テスト対象
            int[] checkCounts;                   // 調べる正解数
            int[] expectedKinds;                 // そのときに出るはずの種類の数
            HashSet<GlyphStyleData> seenStyles;  // 実際に出たスタイル
            int k;                               // 調べる正解数の添字
            int i;                               // ループ用の添字

            // 準備
            first = CreateStyle(1f, 1f, 1f);
            second = CreateStyle(1f, 1f, 1f);
            third = CreateStyle(1f, 1f, 1f);
            SetInt(second, "unlockCorrectCount", 3);
            SetInt(third, "unlockCorrectCount", 6);
            generator = CreateGenerator(new List<GlyphStyleData> { first, second, third }, 1000);
            checkCounts = new[] { 0, 2, 3, 5, 6, 30 };
            expectedKinds = new[] { 1, 1, 2, 2, 3, 3 };

            for (k = 0; k < checkCounts.Length; k++)
            {
                // 実行: その正解数で 3000 回抽選し、出たスタイルの種類を数える
                seenStyles = new HashSet<GlyphStyleData>();
                for (i = 0; i < 3000; i++)
                {
                    seenStyles.Add(generator.Generate(checkCounts[k]).Style);
                }

                // 確認
                Assert.AreEqual(expectedKinds[k], seenStyles.Count, $"正解数 {checkCounts[k]} のときの種類の数");
            }
        }

        /// <summary>
        /// 1問目から出るスタイル（解禁する正解数が 0）が1つもないと、1問目を出せないので例外になる
        /// </summary>
        [Test]
        public void Constructor_NoStyleUnlockedAtStart_ThrowsArgumentException()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData style;   // 3問正解で解禁されるスタイル（これしかない）

            // 準備
            style = CreateStyle(1f, 1f, 1f);
            SetInt(style, "unlockCorrectCount", 3);

            // 実行・確認
            Assert.Throws<ArgumentException>(() => CreateGenerator(new List<GlyphStyleData> { style }, 1100));
        }

        /// <summary>
        /// 1問目から出るスタイルが、どれも出やすさ 0 なら、1問目を出せないので例外になる
        /// （あとで解禁されるスタイルに出やすさがあっても、1問目には使えない）
        /// </summary>
        [Test]
        public void Constructor_StartStylesAllWeightZero_ThrowsArgumentException()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData start;   // 1問目から出るが、出やすさ 0 のスタイル
            GlyphStyleData later;   // 出やすさはあるが、3問正解まで出ないスタイル

            // 準備
            start = CreateStyle(0f, 1f, 1f);
            later = CreateStyle(1f, 1f, 1f);
            SetInt(later, "unlockCorrectCount", 3);

            // 実行・確認
            Assert.Throws<ArgumentException>(() => CreateGenerator(new List<GlyphStyleData> { start, later }, 1200));
        }

        /// <summary>
        /// 正解数がマイナスなのは呼び出し側のミスなので、例外になる
        /// </summary>
        [Test]
        public void Generate_NegativeCorrectCount_ThrowsArgumentOutOfRangeException()
        {
            // ローカル変数は関数の先頭で宣言する
            QuestionGenerator generator;   // テスト対象

            // 準備
            generator = CreateGenerator(new List<GlyphStyleData> { CreateStyle(1f, 1f, 1f) }, 1300);

            // 実行・確認
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(-1));
        }

        /// <summary>
        /// テスト用のスタイルを作る（出やすさと大きさの範囲だけを設定）
        /// </summary>
        /// <param name="weight">出やすさ</param>
        /// <param name="minScale">大きさの最小倍率</param>
        /// <param name="maxScale">大きさの最大倍率</param>
        /// <returns>作ったスタイル（テストの最後に消される）</returns>
        private GlyphStyleData CreateStyle(float weight, float minScale, float maxScale)
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData style;             // 作るスタイル
            SerializedObject serialized;      // Inspector と同じ方法で値を書き換えるための入れ物

            style = ScriptableObject.CreateInstance<GlyphStyleData>();
            createdStyles.Add(style);

            // SerializeField の値は、Inspector と同じく SerializedObject 経由で設定する
            serialized = new SerializedObject(style);
            serialized.FindProperty("weight").floatValue = weight;
            serialized.FindProperty("minScale").floatValue = minScale;
            serialized.FindProperty("maxScale").floatValue = maxScale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return style;
        }

        /// <summary>
        /// テスト用に、スタイルの文字の設定を書き換える
        /// </summary>
        /// <param name="style">書き換えるスタイル</param>
        /// <param name="propertyName">項目名</param>
        /// <param name="value">設定する文字</param>
        private static void SetString(GlyphStyleData style, string propertyName, string value)
        {
            // ローカル変数は関数の先頭で宣言する
            SerializedObject serialized;   // Inspector と同じ方法で値を書き換えるための入れ物

            serialized = new SerializedObject(style);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// テスト用に、スタイルの整数の設定を書き換える
        /// </summary>
        /// <param name="style">書き換えるスタイル</param>
        /// <param name="propertyName">項目名</param>
        /// <param name="value">設定する値</param>
        private static void SetInt(GlyphStyleData style, string propertyName, int value)
        {
            // ローカル変数は関数の先頭で宣言する
            SerializedObject serialized;   // Inspector と同じ方法で値を書き換えるための入れ物

            serialized = new SerializedObject(style);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// テスト用の QuestionGenerator を作る（答えの抽選も固定シードで準備する）
        /// </summary>
        /// <param name="styles">使うスタイル</param>
        /// <param name="seed">乱数のシード</param>
        /// <returns>作った QuestionGenerator</returns>
        private static QuestionGenerator CreateGenerator(List<GlyphStyleData> styles, int seed)
        {
            // ローカル変数は関数の先頭で宣言する
            System.Random random;        // 答えとスタイルの抽選に使う乱数
            AnswerPicker answerPicker;   // 答えの抽選

            random = new System.Random(seed);
            answerPicker = new AnswerPicker();
            answerPicker.Initialize(StreakLimit, random);
            return new QuestionGenerator(answerPicker, styles, random);
        }
    }
}
