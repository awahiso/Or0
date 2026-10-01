using System;
using UnityEngine;

namespace OrZero
{
    /// <summary>
    /// ランキングの読み書き（PlayerPrefs の1つのキーに、JSON で保存する）。Game シーンとタイトル画面の両方から使う。
    /// 保存データが壊れていても、ゲームを止めずに空のランキングから始める
    /// </summary>
    public static class RankingStorage
    {
        /// <summary>
        /// 保存に使うキー。どのシーンからも同じキーを使うので、Inspector ではなくここで決める。
        /// 変えると今までの記録が読めなくなるので変えない
        /// </summary>
        public const string SaveKey = "OrZero.Ranking";

        /// <summary>
        /// 保存されているランキングを読み込む（まだなければ空のランキング）
        /// </summary>
        /// <param name="size">残す件数（1以上。GameBalanceData の値）</param>
        /// <returns>読み込んだランキング</returns>
        public static RankingTable Load(int size)
        {
            // ローカル変数は関数の先頭で宣言する
            RankingTable empty;   // 読めなかったときに使う空のランキング
            string json;          // 保存されている JSON

            // 件数の設定ミスはここで例外になる（壊れたデータの扱いと混ざらないように、先に作っておく）
            empty = new RankingTable(size);

            json = PlayerPrefs.GetString(SaveKey, "");
            try
            {
                return RankingTable.FromJson(json, size);
            }
            catch (ArgumentException e)
            {
                // 保存データが壊れていたら、警告を出して空から始める（次に保存したときに上書きされる）
                Debug.LogWarning($"RankingStorage: ランキングの保存データが読めなかったので、空のランキングから始めます（{e.Message}）");
                return empty;
            }
        }

        /// <summary>
        /// ランキングを保存する（すぐにディスクへ書き込む）
        /// </summary>
        /// <param name="table">保存するランキング</param>
        public static void Save(RankingTable table)
        {
            // 保存するものがないのは呼び出し側のミスなので止める
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            PlayerPrefs.SetString(SaveKey, table.ToJson());

            // アプリが落ちても記録が消えないように、すぐに書き込む（1プレイに1回だけなので重くない）
            PlayerPrefs.Save();
        }
    }
}
