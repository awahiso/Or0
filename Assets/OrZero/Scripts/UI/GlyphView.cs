using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// 3D空間の中央に置いた出題文字（TextMesh Pro）を表示するクラス（SPEC §1.2・§1.4）。
    /// 毎問、スタイルに従って書体・文字・大きさ・太さ・イタリック・アウトライン・色を変え、
    /// 出題のたびに文字を一瞬小さくしてから元の大きさに戻す（切り替わり演出）。
    /// 大きさは縦横同じ比率でしか変えず、回転もさせない（公平性ルール2）
    /// </summary>
    public class GlyphView : MonoBehaviour
    {
        // TMP のシェーダーの項目（Distance Field シェーダーの名前）
        private static readonly int FaceDilateId = Shader.PropertyToID("_FaceDilate");       // 太さ
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");   // アウトラインの幅
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");   // アウトラインの色

        // ===== 参照・見た目の設定（Inspector で設定） =====
        [SerializeField] private TMP_Text glyphText;                          // 出題文字（3D空間に置いた TextMeshPro）
        [SerializeField, Range(0.5f, 1f)] private float popStartScale = 0.85f;   // 切り替わり演出: 出題した瞬間の大きさ（元の大きさに対する倍率）
        [SerializeField, Min(0f)] private float popSeconds = 0.08f;           // 切り替わり演出: 元の大きさに戻るまでの時間（秒）。入力の受付待ちと同じ長さにしておくと自然

        // ===== 実行時の状態（確認用に Inspector へ表示） =====
        [SerializeField] private float targetScale = 1f;      // この問題の大きさ（スタイルで抽選された倍率）
        [SerializeField] private float popElapsedSeconds;     // 切り替わり演出の経過時間（秒）

        // スタイルごとに作ったマテリアル（太さやアウトラインを変えるため）。毎問作るとマテリアルが増え続けるので使い回す。
        // Dictionary は Unity が保存できない型なので、SerializeField にはしない
        private readonly Dictionary<GlyphStyleData, Material> materialCache = new Dictionary<GlyphStyleData, Material>();

        /// <summary>
        /// 毎フレーム、切り替わり演出の途中なら大きさを元に近づける
        /// </summary>
        private void Update()
        {
            // 演出が終わっていれば何もしない
            if (popElapsedSeconds >= popSeconds)
            {
                return;
            }

            popElapsedSeconds += Time.deltaTime;
            ApplyScale(CalculatePopScale(popElapsedSeconds, popSeconds, popStartScale));
        }

        /// <summary>
        /// 破棄されるときに、自分で作ったマテリアルも破棄する（作ったマテリアルは自動では消えないため）
        /// </summary>
        private void OnDestroy()
        {
            // ローカル変数は関数の先頭で宣言する
            List<Material> materials;   // 作ったマテリアルの一覧（破棄用）
            int i;                      // ループ用の添字

            materials = new List<Material>(materialCache.Values);
            for (i = 0; i < materials.Count; i++)
            {
                Destroy(materials[i]);
            }
            materialCache.Clear();
        }

        /// <summary>
        /// 1問分の出題文字を表示する（スタイルを反映し、切り替わり演出を始める）
        /// </summary>
        /// <param name="question">出題内容</param>
        public void Show(QuestionData question)
        {
            ApplyStyle(question.Style);
            glyphText.text = question.Text;
            glyphText.enabled = true;

            // 切り替わり演出: いったん小さくしてから、Update で元の大きさへ戻していく
            targetScale = question.Scale;
            popElapsedSeconds = 0f;
            ApplyScale(CalculatePopScale(0f, popSeconds, popStartScale));
        }

        /// <summary>
        /// 切り替わり演出の大きさを計算する（最初に速く、最後はゆっくり元の大きさへ戻る）
        /// </summary>
        /// <param name="elapsedSeconds">演出の経過時間（秒）</param>
        /// <param name="durationSeconds">元の大きさに戻るまでの時間（秒。0 なら演出なし）</param>
        /// <param name="startScale">出題した瞬間の大きさ（元の大きさに対する倍率）</param>
        /// <returns>元の大きさに対する倍率（startScale〜1）</returns>
        public static float CalculatePopScale(float elapsedSeconds, float durationSeconds, float startScale)
        {
            // ローカル変数は関数の先頭で宣言する
            float progress;   // 演出の進み具合（0〜1）
            float eased;      // 進み具合に緩急をつけた値（0〜1）

            // 演出の時間が 0 なら、最初から元の大きさ
            if (durationSeconds <= 0f)
            {
                return 1f;
            }

            progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            eased = 1f - (1f - progress) * (1f - progress);
            return startScale + (1f - startScale) * eased;
        }

        /// <summary>
        /// スタイルの見た目を出題文字に反映する（書体・マテリアル・イタリック・色）
        /// </summary>
        /// <param name="style">反映するスタイル</param>
        private void ApplyStyle(GlyphStyleData style)
        {
            // 書体がないスタイルは読み込み時に外しているが、念のため今の書体のままにする
            if (style.FontAsset != null)
            {
                glyphText.font = style.FontAsset;
                glyphText.fontSharedMaterial = GetMaterial(style);

                // マテリアルの太さやアウトラインが変わったので、文字の余白を計算し直す（端が欠けないように）
                glyphText.UpdateMeshPadding();
            }

            glyphText.fontStyle = style.UseItalic ? FontStyles.Italic : FontStyles.Normal;
            glyphText.color = style.FaceColor;
        }

        /// <summary>
        /// スタイルに合ったマテリアルを返す。太さもアウトラインも使わないなら書体のマテリアルをそのまま、
        /// 使うならスタイルごとに1つだけ作って使い回す
        /// </summary>
        /// <param name="style">スタイル</param>
        /// <returns>出題文字に使うマテリアル</returns>
        private Material GetMaterial(GlyphStyleData style)
        {
            // ローカル変数は関数の先頭で宣言する
            Material material;   // このスタイル用のマテリアル

            // 太さもアウトラインも使わないスタイルは、書体のマテリアルをそのまま使う（複製を作らない）
            if (Mathf.Approximately(style.FaceDilate, 0f) && Mathf.Approximately(style.OutlineWidth, 0f))
            {
                return style.FontAsset.material;
            }

            // すでに作ってあれば使い回す
            if (materialCache.TryGetValue(style, out material))
            {
                return material;
            }

            // 書体のマテリアルを元に、このスタイル専用のマテリアルを1つだけ作る
            // （書体のマテリアルそのものを書き換えると、同じ書体を使うほかのスタイルやアセットまで変わってしまうため）
            material = new Material(style.FontAsset.material);
            material.name = $"{style.FontAsset.material.name} ({style.name})";
            material.SetFloat(FaceDilateId, style.FaceDilate);
            material.SetFloat(OutlineWidthId, style.OutlineWidth);
            material.SetColor(OutlineColorId, style.OutlineColor);
            if (style.OutlineWidth > 0f)
            {
                // モバイル用の TMP シェーダーでは、アウトラインを出すのにこのキーワードが必要
                material.EnableKeyword("OUTLINE_ON");
            }

            materialCache.Add(style, material);
            return material;
        }

        /// <summary>
        /// 出題文字の大きさを設定する（縦横同じ比率。スタイルの大きさ × 切り替わり演出の倍率）
        /// </summary>
        /// <param name="popScale">切り替わり演出の倍率</param>
        private void ApplyScale(float popScale)
        {
            transform.localScale = Vector3.one * (targetScale * popScale);
        }
    }
}
