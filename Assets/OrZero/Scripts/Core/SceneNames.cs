namespace OrZero
{
    /// <summary>
    /// シーン名（Build Profiles の Scene List に入れたシーンの名前と同じにする）。
    /// シーンを読み込むときは文字を直接書かず、ここの定数を使う（綴りの間違いを防ぐため）
    /// </summary>
    public static class SceneNames
    {
        /// <summary>タイトル画面のシーン（Scenes/Title.unity）。ビルドではこのシーンから始まる（Scene List の先頭）</summary>
        public const string Title = "Title";

        /// <summary>ゲーム本編のシーン（Scenes/Game.unity）</summary>
        public const string Game = "Game";
    }
}
