namespace OrZero
{
    /// <summary>
    /// 1プレイが終わった理由（リザルトの見出しと、表示する中身を切り替えるのに使う）
    /// </summary>
    public enum GameEndReason
    {
        Miss,     // 回答を間違えた（GAME OVER）
        TimeUp,   // 残り時間が 0 になった（TIME UP）
    }
}
