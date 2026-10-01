using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// ランキング（上位 N 件のスコア。SPEC のランキングの節）。
    /// スコアの高い順に並べ、同じスコアは先に入れた記録を上にする。0点以下は残さない。名前は持たない。
    /// ランクは表示するときにスコアから決めるので、ここではスコアだけを持つ。
    /// 保存用の JSON への変換もここで行う（PlayerPrefs への読み書きは RankingStorage）
    /// </summary>
    public class RankingTable
    {
        // ===== 中身（残す件数は作るときに決まり、あとから変えない） =====
        private readonly int size;           // 残す件数（件）
        private readonly List<int> scores;   // スコア（点。高い順。同点は先に入れた記録が上）

        /// <summary>
        /// 空のランキングを作る
        /// </summary>
        /// <param name="size">残す件数（1以上）</param>
        public RankingTable(int size)
        {
            // 1件も残せないランキングは設定ミスなので止める
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "ランキングに残す件数は1以上にしてください");
            }

            this.size = size;
            scores = new List<int>(size + 1);
        }

        /// <summary>残す件数（件）</summary>
        public int Size => size;

        /// <summary>スコア（点。高い順）</summary>
        public IReadOnlyList<int> Scores => scores;

        /// <summary>1位のスコア（ベストスコア）。記録がなければ 0</summary>
        public int BestScore => scores.Count > 0 ? scores[0] : 0;

        /// <summary>
        /// 1プレイの記録を入れる。入った順位（0 が1位＝NEW RECORD）を返す。
        /// 0点以下、またはランキングに入らなかったときは -1 を返し、ランキングは変えない
        /// </summary>
        /// <param name="score">スコア（点）</param>
        /// <returns>入った順位（0 が1位）。入らなければ -1</returns>
        public int Insert(int score)
        {
            // ローカル変数は関数の先頭で宣言する
            int position;   // 入れる位置（0 が1位）

            // 0点以下の記録は残さない
            if (score <= 0)
            {
                return -1;
            }

            // 同じスコアの記録より下に入れる（先に出した記録を上にする）
            position = 0;
            while (position < scores.Count && scores[position] >= score)
            {
                position++;
            }

            // 残す件数より下なら、ランキングに入らない
            if (position >= size)
            {
                return -1;
            }

            // 入れて、件数を超えた分（いちばん下）を押し出す
            scores.Insert(position, score);
            if (scores.Count > size)
            {
                scores.RemoveAt(scores.Count - 1);
            }
            return position;
        }

        /// <summary>
        /// 保存用の JSON にする（形は {"scores":[...]}）
        /// </summary>
        /// <returns>JSON の文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(new RankingSaveData(scores.ToArray()));
        }

        /// <summary>
        /// 保存用の JSON からランキングを作る。並びが崩れていても高い順に戻し、0点以下と件数を超えた分は外す。
        /// 空の文字なら空のランキングを返す。壊れた JSON は ArgumentException になる（扱いは呼び出し側で決める）
        /// </summary>
        /// <param name="json">保存用の JSON（null か空ならまだ記録なし）</param>
        /// <param name="size">残す件数（1以上）</param>
        /// <returns>読み込んだランキング</returns>
        public static RankingTable FromJson(string json, int size)
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable table;      // 読み込んだランキング
            RankingSaveData data;    // JSON から戻した保存データ
            int i;                   // ループ用の添字

            table = new RankingTable(size);
            if (string.IsNullOrEmpty(json))
            {
                return table;
            }

            // 壊れた JSON なら、ここで JsonUtility が ArgumentException を出す
            data = JsonUtility.FromJson<RankingSaveData>(json);
            if (data == null || data.Scores == null)
            {
                return table;
            }

            // 保存されていた順に入れ直す（Insert の決まりで、高い順・0点以下なし・件数までに整う。同点の順も保たれる）
            for (i = 0; i < data.Scores.Length; i++)
            {
                table.Insert(data.Scores[i]);
            }
            return table;
        }
    }
}
