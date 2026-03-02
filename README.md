# AuctionHouseManager

FFXI プライベートサーバー（LandSandBoat / XiDB）向けの競売所自動出品管理ツールです。

## 概要

このツールは、指定されたアイテムを自動的に競売所に出品し、在庫を維持します。また、出品と同時に最初の1つを自動的に落札扱いにすることで、プレイヤーが競売所で価格履歴を確認できるようにします。

## 主な機能

- **自動出品**: `custom_item_book` テーブルに登録されているアイテムが競売所にない場合、自動的に出品します。
- **一括出品**: 対象のアイテムを一度に 11 個出品します。
- **履歴作成**: 出品したアイテムのうち 1 つを即座に落札（売却済み）としてマークし、履歴と価格を表示させます。
- **定期実行**: 1 分間隔でデータベースをチェックし、在庫が切れているアイテムを補充します。
- **AHカテゴリ登録**: `auction_house_items` テーブルへ自動登録し、ゲーム内の競売メニューにアイテムが表示されるようにします。

## 動作要件

- .NET 8.0 SDK 以降
- MySQL / MariaDB (XiDB)
- `custom_item_book` テーブルがデータベースに存在すること

## セットアップ

### 1. データベースの準備

出品したいアイテムを管理するために、以下の構成の `custom_item_book` テーブルを作成してください。

```sql
CREATE TABLE `custom_item_book` (
  `itemId` int(11) NOT NULL,
  PRIMARY KEY (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

このテーブルに出品したいアイテムの ID を追加します。

### 2. 設定の変更

`AuctionHouseManager.cs` 内の `ConnectionString` を、自身の環境に合わせて修正してください。

```csharp
public static string ConnectionString { get; set; } = "server=localhost;port=3306;user=root;password=root;database=xidb";
```

### 3. ビルドと実行

プロジェクトディレクトリで以下のコマンドを実行します。

```bash
dotnet build
dotnet run
```

## 注意事項

- デフォルトのセラー名は `CustomAH1`、セラー ID は `1000000` に設定されています。
- 価格は `item_basic` テーブルの `BaseSell`（標準売却価格）の 10 倍に設定されます（スタックアイテムの場合はスタック数も考慮されます）。
- 競売に出品不可能なアイテム（`aH` フラグが 99 以上）は無視されます。

## 免責事項

このツールは LandSandBoat などのプライベートサーバー環境での使用を想定しています。
