using NUnit.Framework;

namespace OrZero.Tests
{
    /// <summary>
    /// TitleScreenController（タイトル画面）の、表示する文字の組み立てのテスト（EditMode）
    /// ベストスコアの表示（記録がないときと、3桁ごとのカンマ）を確かめる
    /// </summary>
    public class TitleScreenControllerTests
    {
        /// <summary>
        /// 記録がまだない（ベストスコアが 0）ときは、数字の代わりにダッシュを出す
        /// </summary>
        [Test]
        public void FormatBestScore_NoRecord_ShowsDashes()
        {
            // 実行・確認
            Assert.AreEqual("BEST  ---", TitleScreenController.FormatBestScore(0));
        }

        /// <summary>
        /// 記録があれば、3桁ごとにカンマを入れて出す
        /// </summary>
        [Test]
        public void FormatBestScore_WithRecord_ShowsScoreWithComma()
        {
            // 実行・確認
            Assert.AreEqual("BEST  12,500", TitleScreenController.FormatBestScore(12500));
            Assert.AreEqual("BEST  190", TitleScreenController.FormatBestScore(190));
        }
    }
}
