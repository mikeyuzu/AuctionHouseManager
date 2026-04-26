
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuctionHouseManager
{
    internal class AuctionHouseManager
    {
        public static string ConnectionString { get; set; } = "server=localhost;port=3306;user=root;password=root;database=xidb";
        private const int SellerId = 1000000;
        private const string SellerName = "CustomAH1";

        /// <summary>
        /// 競売に出品するタスク
        /// </summary>
        /// <returns></returns>
        public static async Task ProcessAuctionItemsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // 1. custom_item_book と auction_house から itemId を取得
                    var itemBookIds = GetItemIdsFromTableAsync("custom_item_book", "itemId", string.Empty);
                    var auctionIds = GetItemIdsFromTableAsync("auction_house", "itemid", "sale = 0");

                    // 2. 出品対象の itemId を抽出
                    var itemsToAuction = new HashSet<int>(itemBookIds);
                    itemsToAuction.ExceptWith(auctionIds);

                    if (itemsToAuction.Count == 0)
                    {
                        return; // 出品するアイテムがない
                    }

                    // 3. 対象アイテムをオークションに出品
                    foreach (var itemId in itemsToAuction)
                    {
                        ListItemAsync(itemId);
                    }
                }
                catch (Exception ex)
                {
                    // エラーハンドリング（ログ出力など）
                    Console.WriteLine($"An error occurred in ProcessAuctionItems: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// アイテムをオークションに出品
        /// </summary>
        /// <param name="itemId"></param>
        private static void ListItemAsync(int itemId)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // アイテム情報を取得
            string query = "SELECT stackSize, aH, BaseSell FROM item_basic WHERE itemid = @itemId";
            MySqlCommand cmd = new(query, connection);
            cmd.Parameters.AddWithValue("@itemId", itemId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return;

            var auctionHouseValue = reader.GetInt32("aH");
            if (auctionHouseValue >= 99) return; // 出品不可アイテム

            var baseSellPrice = reader.GetInt32("BaseSell");
            var stackSize = reader.GetInt32("stackSize");
            reader.Close();

            // 価格とスタックフラグを計算
            bool isStackable = stackSize > 1;
            int price = baseSellPrice * 10;
            if (isStackable)
            {
                price *= stackSize;
            }

            // トランザクション開始
            using var transaction = connection.BeginTransaction();
            try
            {
                long firstInsertedId = -1;
                // 11品出品
                for (int i = 0; i < 11; i++)
                {
                    var insertCmd = new MySqlCommand("INSERT INTO auction_house (itemid, stack, seller, seller_name, date, price) VALUES (@itemid, @stack, @seller, @seller_name, @date, @price)", connection, transaction);
                    insertCmd.Parameters.AddWithValue("@itemid", itemId);
                    insertCmd.Parameters.AddWithValue("@stack", isStackable ? 1 : 0);
                    insertCmd.Parameters.AddWithValue("@seller", SellerId);
                    insertCmd.Parameters.AddWithValue("@seller_name", SellerName);
                    insertCmd.Parameters.AddWithValue("@date", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    insertCmd.Parameters.AddWithValue("@price", price);
                    insertCmd.ExecuteNonQuery();
                    if (i == 0)
                    {
                        firstInsertedId = insertCmd.LastInsertedId;
                    }
                }

                // 1品目を落札済みにする
                if (firstInsertedId != -1)
                {
                    var updateCmd = new MySqlCommand("UPDATE auction_house SET buyer_name = @buyer_name, sale = @sale, sell_date = @sell_date WHERE id = @id", connection, transaction);
                    updateCmd.Parameters.AddWithValue("@buyer_name", SellerName); // 自分自身が落札
                    updateCmd.Parameters.AddWithValue("@sale", price);
                    updateCmd.Parameters.AddWithValue("@sell_date", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    updateCmd.Parameters.AddWithValue("@id", firstInsertedId);
                    updateCmd.ExecuteNonQuery();
                }

                // auction_house_items に登録
                var insertAhItemsCmd = new MySqlCommand("INSERT IGNORE INTO auction_house_items (itemid) VALUES (@itemid)", connection, transaction);
                insertAhItemsCmd.Parameters.AddWithValue("@itemid", itemId);
                insertAhItemsCmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw; // エラーを再スロー
            }
        }

        /// <summary>
        /// DBからアイテム情報を取得する
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="whereName"></param>
        /// <returns></returns>
        private static HashSet<int> GetItemIdsFromTableAsync(string tableName, string columnName, string whereName)
        {
            var ids = new HashSet<int>();
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                string query = $"SELECT DISTINCT {columnName} FROM {tableName}";
                if (whereName != string.Empty)
                {
                    query += $" WHERE {whereName}";
                }

                var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ids.Add(reader.GetInt32(0));
                }
            }
            return ids;
        }
    }
}
