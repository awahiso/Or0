using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// 出題スタイル1種類（SPEC §1.4）。1つのスタイルで英字側と数字側の両方を出す。
    /// `Assets/OrZero/Resources/GlyphStyles/` に置くと、起動時に自動で読み込まれる（T9）。
    /// 入力ミスと見分けにくさ（英字側と数字側の縦横比の差が 0.05 未満）は、Inspector で値を変えたときに警告する
    /// </summary>
    [CreateAssetMenu(fileName = "GlyphStyle", menuName = "OrZero/Glyph Style")]
    public class GlyphStyleData : ScriptableObject
    {
        /// <summary>英字側と数字側の縦横比の差が、これ未満だと見分けにくい（SPEC §1.4 ルール3）</summary>
        public const float HardToDistinguishThreshold = 0.05f;

        // ===== 基本 =====
        [SerializeField] private string displayName = "";           // 管理名（一覧で見分けるための名前）
        [SerializeField] private TMP_FontAsset fontAsset;           // 書体（TMP フォントアセット）
        [SerializeField] private string letterText = "O";           // 英字側に表示する文字（1文字）
        [SerializeField] private string digitText = "0";            // 数字側に表示する文字（1文字）
        [SerializeField, Min(0f)] private float weight = 1f;        // 出やすさ（抽選の重み。0 にすると出なくなる）

        // ===== 見た目 =====
        [SerializeField, Min(0.1f)] private float minScale = 1f;         // 大きさの最小倍率（縦横同じ比率で変える）
        [SerializeField, Min(0.1f)] private float maxScale = 1f;         // 大きさの最大倍率（縦横同じ比率で変える）
        [SerializeField, Range(-0.5f, 0.5f)] private float faceDilate;   // 太さ（TMP の Face Dilate。マイナスで細く、プラスで太く）
        [SerializeField] private bool useItalic;                         // イタリック（傾き）にするか。丸さが少し変わるので使いすぎない
        [SerializeField, Range(0f, 1f)] private float outlineWidth;      // アウトラインの幅（0 でなし）
        [SerializeField] private Color outlineColor = Color.white;       // アウトラインの色
        [SerializeField] private Color faceColor = new Color32(0x1F, 0x20, 0x26, 0xFF);   // 文字の色（既定は SPEC §1.5 の出題文字の色）

        // ===== 装飾（T10 で使う。0 にするとその装飾は出ない） =====
        [SerializeField, Min(0)] private int decorationDotCount;        // 文字の周りに散らすドットの数（個）
        [SerializeField, Min(0)] private int decorationLineCount;       // 文字の周りの放射状の線の本数（本）
        [SerializeField, Range(0f, 1f)] private float ghostAlpha;       // 少しずらして重ねる影（ゴースト）の濃さ

        /// <summary>管理名</summary>
        public string DisplayName => displayName;

        /// <summary>書体（TMP フォントアセット）</summary>
        public TMP_FontAsset FontAsset => fontAsset;

        /// <summary>出やすさ（抽選の重み）</summary>
        public float Weight => weight;

        /// <summary>大きさの最小倍率</summary>
        public float MinScale => minScale;

        /// <summary>大きさの最大倍率</summary>
        public float MaxScale => maxScale;

        /// <summary>太さ（TMP の Face Dilate）</summary>
        public float FaceDilate => faceDilate;

        /// <summary>イタリックにするか</summary>
        public bool UseItalic => useItalic;

        /// <summary>アウトラインの幅（0 でなし）</summary>
        public float OutlineWidth => outlineWidth;

        /// <summary>アウトラインの色</summary>
        public Color OutlineColor => outlineColor;

        /// <summary>文字の色</summary>
        public Color FaceColor => faceColor;

        /// <summary>文字の周りに散らすドットの数（個）</summary>
        public int DecorationDotCount => decorationDotCount;

        /// <summary>放射状の線の本数（本）</summary>
        public int DecorationLineCount => decorationLineCount;

        /// <summary>ゴースト影の濃さ（0 でなし）</summary>
        public float GhostAlpha => ghostAlpha;

        /// <summary>
        /// 答えの種類に応じて、表示する文字を返す
        /// </summary>
        /// <param name="answer">答えの種類</param>
        /// <returns>英字側なら letterText、数字側なら digitText</returns>
        public string GetText(GlyphType answer)
        {
            return answer == GlyphType.LetterO ? letterText : digitText;
        }

        /// <summary>
        /// 英字側と数字側の縦横比（幅÷高さ）の差を計算する（英字側 − 数字側）。
        /// O の方が丸ければプラス、0 の方が丸ければマイナスになる
        /// </summary>
        /// <param name="letterWidth">英字側の文字の幅</param>
        /// <param name="letterHeight">英字側の文字の高さ（0 より大きい）</param>
        /// <param name="digitWidth">数字側の文字の幅</param>
        /// <param name="digitHeight">数字側の文字の高さ（0 より大きい）</param>
        /// <returns>縦横比の差</returns>
        public static float CalculateAspectDifference(float letterWidth, float letterHeight, float digitWidth, float digitHeight)
        {
            // 高さが 0 以下だと縦横比を計算できない（書体の情報がおかしい）
            if (letterHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(letterHeight), letterHeight, "文字の高さは0より大きくしてください");
            }
            if (digitHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(digitHeight), digitHeight, "文字の高さは0より大きくしてください");
            }

            return letterWidth / letterHeight - digitWidth / digitHeight;
        }

        /// <summary>
        /// 縦横比の差から、見分けにくいかどうかを判定する（差の大きさが 0.05 未満なら見分けにくい）
        /// </summary>
        /// <param name="aspectDifference">縦横比の差（CalculateAspectDifference の結果）</param>
        /// <returns>見分けにくければ true</returns>
        public static bool IsHardToDistinguish(float aspectDifference)
        {
            return Math.Abs(aspectDifference) < HardToDistinguishThreshold;
        }

        /// <summary>
        /// 表示する文字の設定に問題がないか調べる（1文字であること、英字側は英字・数字側は数字であること）
        /// </summary>
        /// <param name="letter">英字側に表示する文字</param>
        /// <param name="digit">数字側に表示する文字</param>
        /// <returns>見つかった問題（なければ空）</returns>
        public static List<string> FindTextProblems(string letter, string digit)
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> problems;   // 見つかった問題

            problems = new List<string>();

            // 英字側: 1文字の英字（O・o・全角のＯ なども英字として扱う）
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
            {
                problems.Add("英字側に表示する文字は1文字にしてください");
            }
            else if (!char.IsLetter(letter[0]))
            {
                problems.Add($"英字側の「{letter}」が英字ではありません");
            }

            // 数字側: 1文字の数字（0・全角の０ なども数字として扱う）
            if (string.IsNullOrEmpty(digit) || digit.Length != 1)
            {
                problems.Add("数字側に表示する文字は1文字にしてください");
            }
            else if (!char.IsDigit(digit[0]))
            {
                problems.Add($"数字側の「{digit}」が数字ではありません");
            }

            return problems;
        }

        /// <summary>
        /// このスタイルの問題をすべて調べる（入力ミスと見分けにくさ）。
        /// 書体に文字がなければ、書体に文字を読み込ませてから調べる（Dynamic の書体は、ここで文字が追加される）
        /// </summary>
        /// <returns>見つかった問題（なければ空）</returns>
        public List<string> CollectProblems()
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> problems;   // 見つかった問題

            problems = FindTextProblems(letterText, digitText);

            if (minScale > maxScale)
            {
                problems.Add($"大きさの最小（{minScale}）が最大（{maxScale}）より大きくなっています");
            }

            if (fontAsset == null)
            {
                problems.Add("書体（TMP フォントアセット）が未設定です");
                return problems;
            }

            // 文字の設定に問題がなければ、書体の文字の形から見分けやすさを調べる
            if (problems.Count == 0)
            {
                CheckGlyphShapes(problems);
            }

            return problems;
        }

        /// <summary>
        /// 書体に入っている文字の形の情報（幅・高さ）から、英字側と数字側の見分けやすさを調べる
        /// </summary>
        /// <param name="problems">見つかった問題を足していく一覧</param>
        private void CheckGlyphShapes(List<string> problems)
        {
            // ローカル変数は関数の先頭で宣言する
            float letterWidth;    // 英字側の文字の幅
            float letterHeight;   // 英字側の文字の高さ
            float digitWidth;     // 数字側の文字の幅
            float digitHeight;    // 数字側の文字の高さ
            float difference;     // 縦横比の差（英字側 − 数字側）

            if (!TryGetGlyphSize(letterText[0], out letterWidth, out letterHeight))
            {
                problems.Add($"書体に英字側の「{letterText}」が入っていません");
                return;
            }
            if (!TryGetGlyphSize(digitText[0], out digitWidth, out digitHeight))
            {
                problems.Add($"書体に数字側の「{digitText}」が入っていません");
                return;
            }

            difference = CalculateAspectDifference(letterWidth, letterHeight, digitWidth, digitHeight);
            if (IsHardToDistinguish(difference))
            {
                problems.Add($"英字側と数字側の縦横比の差が {Math.Abs(difference):F3} しかなく、見分けにくい書体です（数字側に斜線やドットがなければ使わない）");
            }
            else if (difference < 0f)
            {
                problems.Add($"この書体は数字側の方が英字側より丸い形です（差 {difference:F3}）。「丸い方が O」で見分けると逆になるので、試遊で確かめてください");
            }
        }

        /// <summary>
        /// 書体に入っている1文字の大きさ（幅・高さ）を取り出す
        /// </summary>
        /// <param name="character">調べる文字</param>
        /// <param name="width">文字の幅（取れなければ 0）</param>
        /// <param name="height">文字の高さ（取れなければ 0）</param>
        /// <returns>取り出せたら true</returns>
        private bool TryGetGlyphSize(char character, out float width, out float height)
        {
            // ローカル変数は関数の先頭で宣言する
            TMP_Character tmpCharacter;   // 書体の中の文字の情報

            width = 0f;
            height = 0f;

            // Dynamic の書体は、まだ使っていない文字がないことがあるので、ここで読み込ませる
            if (!fontAsset.HasCharacter(character, false, true))
            {
                return false;
            }
            if (!fontAsset.characterLookupTable.TryGetValue(character, out tmpCharacter) || tmpCharacter.glyph == null)
            {
                return false;
            }

            width = tmpCharacter.glyph.metrics.width;
            height = tmpCharacter.glyph.metrics.height;
            return height > 0f;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector で値を変えたとき（とアセットを読み込んだとき）に呼ばれる。
        /// OnValidate の中で書体（別のアセット）を書き換えると Unity が警告を出すことがあるので、確認は少し後で行う
        /// </summary>
        private void OnValidate()
        {
            // 何度呼ばれても1回だけ確認するように、登録し直す
            UnityEditor.EditorApplication.delayCall -= ReportProblems;
            UnityEditor.EditorApplication.delayCall += ReportProblems;
        }

        /// <summary>
        /// 問題を調べて、見つかったものを Console に警告として出す
        /// </summary>
        private void ReportProblems()
        {
            // ローカル変数は関数の先頭で宣言する
            List<string> problems;   // 見つかった問題
            int i;                   // ループ用の添字

            // 確認を待っている間にアセットが消された場合は何もしない
            if (this == null)
            {
                return;
            }

            problems = CollectProblems();
            for (i = 0; i < problems.Count; i++)
            {
                Debug.LogWarning($"GlyphStyle「{name}」: {problems[i]}", this);
            }
        }
#endif
    }
}
