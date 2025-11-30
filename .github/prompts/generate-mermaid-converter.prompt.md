---
description: 'NextDesignシーケンス図とMermaid形式の完全な双方向変換機能をC#スクリプトで実装'
agent: 'generate-mermaid-converter'
tools: ['vscode', 'edit', 'read', 'search']
---

# NextDesign Mermaid変換拡張機能の実装

NextDesignシーケンス図とMermaid形式の完全な双方向変換機能をC#スクリプトで実装します。メタデータ管理によりNextDesign固有情報を保持し、段階的な実装アプローチで確実に機能を構築します。

## 📥 必須入力

- `${workspaceFolder}`: ワークスペースのルートディレクトリ
- `${input:implementationPhase:実装フェーズを選択してください (1-6)}`: 実装する段階
  - Phase 1: manifest.json + 基本フレームワーク
  - Phase 2: エクスポート基本機能
  - Phase 3: インポート基本機能
  - Phase 4: エクスポート高度機能
  - Phase 5: インポート高度機能
  - Phase 6: 統合テスト・ドキュメント

## 📋 実行ステップ

### ステップ 1: プロジェクト基盤構築

#### 1.1 ディレクトリ構造の作成
1. **基本ディレクトリの作成**
   - `src/` - 拡張機能のソースコード
   - `examples/` - サンプルMermaidファイルとメタデータ
   - `docs/` - ドキュメント

#### 1.2 プロジェクト基本ファイルの作成
1. **README.mdの作成**
   - 概要、機能一覧、インストール手順、使用方法を記述
   - NextDesign拡張機能としての配布方法を含める

2. **.gitignoreの作成**
   - C#スクリプトプロジェクト用の設定
   - Next Design固有のファイル除外

3. **LICENSEの作成**
   - MIT Licenseまたは適切なライセンスを選択

4. **.editorconfigの作成**
   - C#コーディング標準の定義

### ステップ 2: manifest.jsonと防御的コアフレームワーク

#### 2.1 manifest.jsonの作成
1. **基本構造の定義**
   ```json
   {
     "name": "MermaidConverter",
     "version": "1.0.0",
     "description": "NextDesignシーケンス図とMermaid形式の双方向変換",
     "main": "main.cs",
     "lifecycle": "application",
     "extensionPoints": {
       "ribbon": {
         "tabs": [...]
       },
       "commands": [...]
     }
   }
   ```

2. **リボンタブとコマンドの定義**
   ```json
   "ribbon": {
     "tabs": [
       {
         "id": "MermaidConverter.MainTab",
         "label": "Mermaid変換",
         "groups": [
           {
             "id": "MermaidConverter.ConvertGroup",
             "label": "変換",
             "controls": [
               {
                 "id": "MermaidConverter.ExportButton",
                 "type": "Button",
                 "label": "Mermaidへエクスポート",
                 "imageLarge": "resources/export.png",
                 "command": "MermaidConverter.Export"
               },
               {
                 "id": "MermaidConverter.ImportButton",
                 "type": "Button",
                 "label": "Mermaidからインポート",
                 "imageLarge": "resources/import.png",
                 "command": "MermaidConverter.Import"
               }
             ]
           }
         ]
       }
     ]
   },
   "commands": [
     {
       "id": "MermaidConverter.Export",
       "execFunc": "ExportToMermaid"
     },
     {
       "id": "MermaidConverter.Import",
       "execFunc": "ImportFromMermaid"
     }
   ]
   ```

   **重要なポイント**:
   - `ribbon`の下に`tabs`配列を定義
   - 各タブに`id`、`label`、`groups`を指定
   - グループ内に`controls`配列でボタンを定義
   - ボタンの`command`プロパティでコマンドIDを参照
   - コマンドの`execFunc`はmain.csの公開関数名と一致

#### 2.2 main.csの基本フレームワーク
1. **グローバルエラーハンドリング**
   - すべてのNext Design API呼び出しをtry-catchで保護
   - 詳細なエラーメッセージとログ出力

2. **共通ユーティリティ関数**
   - `SafeGetProperty<T>`: 防御的プロパティアクセス
   - `ExecuteWithTransaction`: トランザクション管理
   - `LogInfo/LogWarning/LogError`: 統一されたログ出力

3. **ファイルI/O処理**
   - System.IOを使用したファイル読み書き
   - System.Text.Jsonを使用したメタデータ処理

### ステップ 3: エクスポート機能 - 基本要素

#### 3.1 シーケンス図の走査
1. **ISequenceDiagramの取得**
   - 現在選択されているモデルからシーケンス図を取得
   - モデルタイプの検証

2. **ライフラインの変換**
   - ILifelineの列挙
   - participant/actorの判定ロジック
   - Mermaid構文への変換

3. **基本メッセージの変換**
   - IMessageの列挙と順序付け
   - 同期メッセージ: `->>`
   - 非同期メッセージ: `--)` 
   - 戻りメッセージ: `-->>`
   - Create/Destroyメッセージ

#### 3.2 メタデータの保存
1. **メタデータ構造の定義**
   - diagram: ID、名前、説明、カスタムフィールド
   - lifelines: MermaidID、NextDesignID、名前、タイプ、順序
   - messages: ソース、ターゲット、名前、種類、順序

2. **System.Text.Jsonによる保存**
   - `{SequenceName}_diagram.meta.json`として保存
   - UTF-8エンコーディング

### ステップ 4: エクスポート機能 - 高度な要素

#### 4.1 フラグメントの変換
1. **IInteractionFragmentの処理**
   - alt/loop/opt/parの識別
   - ネスト構造のサポート(最大5レベル)
   - ガード条件の抽出

2. **Mermaid構文の生成**
   ```
   alt 認証成功
     ...
   else 認証失敗
     ...
   end
   ```

#### 4.2 アクティベーション・ノートの処理
1. **アクティベーション**
   - `+` (開始)
   - `-` (終了)

2. **ノート**
   - `Note left of`
   - `Note right of`
   - `Note over`

#### 4.3 未対応要素の処理
1. **警告リストの作成**
   - 変換できない要素をリストアップ
   - メタデータに保存

2. **変換サマリーダイアログ**
   - 成功した要素数
   - 警告の詳細

### ステップ 5: インポート機能 - 完全実装

#### 5.1 Mermaidファイルのパース
1. **構文解析**
   - System.Text.RegularExpressionsを使用
   - participant/actor、メッセージ、フラグメントの抽出
   - ネストフラグメント用Stackの活用

2. **メタデータの検出と読み込み**
   - 同名の`.meta.json`を自動検出
   - メタデータなしの場合は新規ID割り当て

#### 5.2 Next Designモデルの構築
1. **トランザクション管理**
   ```csharp
   App.Workspace.BeginTransaction("Mermaidインポート");
   try {
       // モデル構築処理
       App.Workspace.CommitTransaction();
   } catch {
       App.Workspace.RollbackTransaction();
       throw;
   }
   ```

2. **要素の作成**
   - ISequenceDiagramの作成または取得
   - ILifelineの追加
   - IMessageの追加
   - IInteractionFragmentの追加

#### 5.3 エラーハンドリング
1. **構文エラーの処理**
   - 行番号付きエラーメッセージ
   - 詳細なエラーダイアログ

2. **ロールバック処理**
   - エラー発生時は変更を完全に元に戻す
   - ログファイルへの詳細記録

### ステップ 6: テスト・サンプル・ドキュメント完全整備

#### 6.1 サンプルファイルの作成
1. **基本例: login_diagram.mmd**
   - ユーザー、UI、認証サーバー間のフロー
   - 対応するmeta.jsonファイル

2. **複雑な例: complex_flow_diagram.mmd**
   - フラグメント、ネスト、アクティベーション、ノートを含む
   - 対応するmeta.jsonファイル

#### 6.2 ドキュメントの作成
1. **docs/user-guide.md**
   - インストール手順
   - エクスポート/インポートの使用方法
   - スクリーンショット付き

2. **docs/developer-guide.md**
   - アーキテクチャ概要
   - 各関数の詳細説明
   - 拡張方法

3. **docs/file-format.md**
   - メタデータJSON仕様
   - Mermaid構文のサポート範囲

4. **docs/troubleshooting.md**
   - よくあるエラーと解決方法
   - デバッグ手順

#### 6.3 統合テスト
1. **テストケースの実行**
   - 基本要素のエクスポート/インポート
   - 高度な要素のエクスポート/インポート
   - エラーケースの検証

2. **結果サマリーダイアログの実装**
   - 処理した要素数
   - 警告とエラーの表示

## 📂 ファイル配置

```
MermaidConverter/
├── .github/
│   ├── prompts/
│   │   └── generate-mermaid-converter.prompt.md
│   └── agents/
│       └── generate-mermaid-converter.agent.md
├── src/
│   ├── manifest.json
│   └── main.cs
├── examples/
│   ├── login_diagram.mmd
│   ├── login_diagram.meta.json
│   ├── complex_flow_diagram.mmd
│   └── complex_flow_diagram.meta.json
├── docs/
│   ├── user-guide.md
│   ├── developer-guide.md
│   ├── file-format.md
│   └── troubleshooting.md
├── README.md
├── .gitignore
├── .editorconfig
└── LICENSE
```

## ✅ 成功基準

### 必須項目
- [ ] manifest.jsonが正しく定義されている
- [ ] main.csにすべてのハンドラが公開関数として実装されている
- [ ] エクスポート機能が基本要素をサポート
- [ ] インポート機能が基本要素をサポート
- [ ] トランザクション管理が適切に実装されている
- [ ] エラーハンドリングが防御的プログラミングに従っている
- [ ] メタデータファイルが正しく生成・読み込みされる
- [ ] サンプルファイルが動作する
- [ ] ドキュメントが完全である

### 推奨項目
- [ ] エクスポート機能が高度な要素をサポート
- [ ] インポート機能が高度な要素をサポート
- [ ] 変換サマリーダイアログが実装されている
- [ ] パフォーマンステストが実施されている
- [ ] コードがC#標準に準拠している

## 📚 参考

### 公式ドキュメント
- [Next Design エクステンション開発マニュアル](https://docs.nextdesign.app/extension/)
- [Next Design エクステンション API リファレンス](../../nextdesign_api/_extension_api_intro.md)
- [Next Design エクステンション Samplesリポジトリ](https://github.com/denso-create/NextDesign-Samples)
- [ファイルの準備](https://docs.nextdesign.app/extension/docs/getting-started/dev-with-scripts/manifest)
- [スクリプトによるハンドラの実装](https://docs.nextdesign.app/extension/docs/getting-started/dev-with-scripts/impl-handlers)

### 内部ガイドライン
- [Copilot インストラクション](../copilot-instructions.md)

## 💡 使用例

### 例1: Phase 1の実装

**入力**: `#generate-mermaid-converter` を呼び出し、Phase 1を選択

**実行内容**:
1. ディレクトリ構造を作成
2. manifest.jsonを作成
3. main.csの基本フレームワークを実装
4. 防御的プログラミング用ユーティリティ関数を実装

**出力**:
- 基本的なプロジェクト構造
- コンパイル可能なmain.cs
- 動作するmanifest.json

### 例2: エクスポート機能の使用

**ユーザー操作**:
1. Next Designでシーケンス図を選択
2. リボンタブ「Mermaid変換」→「Mermaidへエクスポート」
3. ファイル名を入力して保存

**結果**:
- `login_diagram.mmd` が生成される
- `login_diagram.meta.json` が生成される
- 完了ダイアログが表示される

### 例3: インポート機能の使用

**ユーザー操作**:
1. Next Designでプロジェクトを開く
2. リボンタブ「Mermaid変換」→「Mermaidからインポート」
3. `.mmd`ファイルを選択

**結果**:
- シーケンス図がNext Designに作成される
- メタデータがあればID・順序が復元される
- 完了ダイアログが表示される

## 🚨 重要な注意事項

### Next Design固有の制約
1. **C#スクリプトのみ**: JavaScriptは未サポート
2. **単一エントリーポイント**: すべてのハンドラを1つのファイルに実装
3. **公開関数**: ハンドラは必ず`public void`で定義
4. **トランザクション必須**: モデル変更時は必ずトランザクション管理

### デバッグ環境の不在
1. **防御的プログラミング**: すべてのAPI呼び出しをtry-catchで保護
2. **詳細なログ**: `Output.WriteLine`を積極的に使用
3. **段階的実装**: 一度に多くの機能を実装せず、フェーズごとに検証

### メタデータファイル
1. **命名規則**: `{SequenceName}_diagram.mmd` + `{SequenceName}_diagram.meta.json`
2. **自動検出**: インポート時に同名の.meta.jsonを自動検出
3. **新規ID生成**: メタデータがない場合でもインポート可能

## 🤝 エージェントとの連携

複雑な実装について相談したい場合は、@generate-mermaid-converter に質問してください。

- API仕様の詳細
- エラーハンドリング戦略
- パフォーマンス最適化
- 拡張機能の追加方法
