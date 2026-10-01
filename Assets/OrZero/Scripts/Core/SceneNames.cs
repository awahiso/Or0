namespace OrZero
{
    /// <summary>
    /// シーン名（Build Profiles の Scene List に入れたシーンの名前と同じにする）。
    /// シーンを読み込むときは文字を直接書かず、ここの定数を使う（綴りの間違いを防ぐため）。
    /// タイトルのシーン名は、タイトル画面（T15）で足す
    /// </summary>
    public static class SceneNames
    {
        /// <summary>ゲーム本編のシーン（Scenes/Game.unity）</summary>
        public const string Game = "Game";
    }
}
