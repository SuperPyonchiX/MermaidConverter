# トラブルシューティング

NextDesign Mermaid Converter で発生する可能性のある問題と解決方法をまとめています。

## 目次

1. [エクスポート関連](#エクスポート関連)
2. [インポート関連](#インポート関連)
3. [ファイル関連](#ファイル関連)
4. [Next Design 関連](#next-design関連)
5. [よくある質問](#よくある質問)

## エクスポート関連

### エクステンションが表示されない

#### 症状
- Next Design のリボンに「Mermaid変換」タブが表示されない

#### 原因
- エクステンションが正しくインストールされていない
- manifest.json にエラーがある

#### 解決方法

1. **インストール確認**:
   ```
   1. Next Design メニュー「ツール」→「オプション」
   2. 左メニュー「エクステンション」
   3. MermaidConverter が一覧に表示されているか確認
   ```

2. **manifest.json 検証**:
   - UTF-8 エンコーディングか確認
   - JSON 構文エラーがないか確認
   - 必須フィールドが存在するか確認

3. **Next Design 再起動**:
   - Next Design を完全に終了
   - タスクマネージャーでプロセスが残っていないか確認
   - 再起動

### 「モデルが選択されていません」エラー

#### 症状
- エクスポートボタンをクリックすると「モデルが選択されていません」と表示される

#### 原因
- シーケンス図が選択されていない
- プロジェクトビューの選択が別の要素になっている

#### 解決方法

1. **プロジェクトビューで選択**:
   ```
   1. プロジェクトビューを開く（表示→プロジェクトビュー）
   2. シーケンス図を左クリックで選択
   3. 選択されていることを確認（ハイライト表示）
   4. エクスポート実行
   ```

2. **エディタで開いている場合**:
   ```
   1. シーケンス図エディタを開く
   2. プロジェクトビューでも対応するシーケンス図を選択
   3. エクスポート実行
   ```

### ファイルが出力されない

#### 症状
- エクスポート完了メッセージが表示されるが、ファイルが存在しない

#### 原因
- 出力フォルダ (`C:\temp\`) が存在しない
- 書き込み権限がない
- ファイルが別のプロセスで開かれている

#### 解決方法

1. **フォルダ作成**:
   ```powershell
   # PowerShell で実行
   New-Item -ItemType Directory -Path "C:\temp" -Force
   ```

2. **権限確認**:
   ```
   1. C:\temp フォルダを右クリック
   2. プロパティ→セキュリティタブ
   3. 現在のユーザーに「書き込み」権限があるか確認
   ```

3. **管理者権限で実行**:
   ```
   1. Next Design を終了
   2. Next Design アイコンを右クリック
   3. 「管理者として実行」
   ```

### ライフラインが0個

#### 症状
- 出力ウィンドウに「ライフライン数: 0」と表示される

#### 原因
- Next Design のモデルが `Lifelines` プロパティを持っていない
- プロパティ名が異なる
- メタモデル定義が想定と異なる

#### 解決方法

1. **ログ確認**:
   ```
   1. Next Design メニュー「表示」→「出力」
   2. [MermaidConverter] で始まるログを探す
   3. エラーメッセージを確認
   ```

2. **プロパティ名確認**:
   - メタモデル定義でライフラインのコレクション名を確認
   - 必要に応じて main.cs の `GetProperty("Lifelines")` を修正

3. **デバッグ出力追加**:
   ```csharp
   // main.cs に追加
   Output.WriteLine("Info", $"[MermaidConverter] Model Type: {model.GetType().FullName}");
   
   foreach (var prop in model.GetType().GetProperties())
   {
       Output.WriteLine("Info", $"[MermaidConverter] Property: {prop.Name}");
   }
   ```

### メッセージが0個

#### 症状
- ライフラインは出力されるが、メッセージが0個

#### 原因
- メッセージの Source または Target が null
- ライフラインマップに存在しないライフラインを参照
- メッセージプロパティ名が異なる

#### 解決方法

1. **メッセージ接続確認**:
   ```
   1. シーケンス図エディタでメッセージを選択
   2. プロパティウィンドウで Source と Target を確認
   3. どちらも設定されているか確認
   ```

2. **ログ確認**:
   - 出力ウィンドウでスキップされたメッセージを確認
   - Source/Target が null の警告を探す

3. **デバッグ出力**:
   ```csharp
   Output.WriteLine("Info", $"[MermaidConverter] Message: {messageName}, Source: {sourceName ?? "null"}, Target: {targetName ?? "null"}");
   ```

### 日本語が文字化け

#### 症状
- 日本語のライフライン名やメッセージ名が文字化けする

#### 原因
- ファイルが UTF-8 で保存されていない
- BOM なし UTF-8 の問題

#### 解決方法

1. **ファイル再保存**:
   ```csharp
   // main.cs で明示的に UTF-8 指定
   using (var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
   {
       writer.Write(mermaidContent.ToString());
   }
   ```

2. **エディタで開いて確認**:
   - VS Code で `.mmd` ファイルを開く
   - 右下のエンコーディング表示が「UTF-8」か確認
   - 異なる場合は「UTF-8 で再保存」

## インポート関連

### Phase 5 未実装

#### 症状
- 「Mermaidからインポート」ボタンが無効、またはエラー

#### 原因
- インポート機能は Phase 5 で実装予定

#### 解決方法
- 現在のバージョンではエクスポートのみサポート
- Phase 5 リリースをお待ちください

## ファイル関連

### メタデータファイルが開けない

#### 症状
- `.meta.json` ファイルを開こうとするとエラー

#### 原因
- JSON 構文エラー
- ファイルが壊れている

#### 解決方法

1. **JSON バリデータ**:
   - [JSONLint](https://jsonlint.com/) でファイルを検証
   - 構文エラーがあれば修正

2. **再エクスポート**:
   - Next Design で再度エクスポート
   - 新しいメタデータファイルを生成

### Mermaid ファイルがプレビューできない

#### 症状
- `.mmd` ファイルを Mermaid エディタで開けない

#### 原因
- Mermaid 構文エラー
- サポートされていない構文

#### 解決方法

1. **オンラインエディタでテスト**:
   - [Mermaid Live Editor](https://mermaid.live/) にコピー&ペースト
   - エラーメッセージを確認

2. **構文チェック**:
   ```mermaid
   # 最小限の構文で確認
   sequenceDiagram
       participant A
       A->>B: Test
   ```

3. **Phase 4 機能の確認**:
   - フラグメント、ノートは Phase 4 で実装予定
   - 現在は基本構文のみサポート

## Next Design 関連

### Next Design が起動しない

#### 症状
- エクステンションインストール後、Next Design が起動しなくなる

#### 原因
- manifest.json または main.cs に構文エラー
- 致命的な実行時エラー

#### 解決方法

1. **セーフモード起動**:
   ```
   1. Next Design を完全に終了
   2. 設定から MermaidConverter を削除
   3. Next Design を再起動
   ```

2. **エラーログ確認**:
   - Next Design のログフォルダを確認
   - エラーメッセージから原因を特定

3. **クリーンインストール**:
   ```
   1. MermaidConverter フォルダをバックアップ
   2. エクステンションリストから削除
   3. 構文エラーを修正
   4. 再インストール
   ```

### 出力ウィンドウが見つからない

#### 症状
- ログを確認したいが、出力ウィンドウが表示されない

#### 解決方法

```
1. Next Design メニューバー「表示」
2. 「出力」または「Output」を選択
3. ウィンドウが表示される
4. [MermaidConverter] で始まる行を探す
```

### エクステンション設定が保存されない

#### 症状
- Next Design 再起動後、エクステンションが無効になる

#### 原因
- 設定ファイルの書き込み権限がない
- Next Design の設定フォルダが読み取り専用

#### 解決方法

1. **管理者権限で設定**:
   ```
   1. Next Design を管理者として実行
   2. エクステンション設定を確認
   3. 通常起動で動作確認
   ```

2. **設定フォルダ権限確認**:
   ```
   1. Next Design 設定フォルダを開く
      通常: %APPDATA%\NextDesign\
   2. プロパティ→セキュリティ
   3. 書き込み権限を確認
   ```

## よくある質問

### Q1: ファイルダイアログで保存先を選べないのはなぜ?

**A**: Phase 2-3 の現在の実装では、固定パス `C:\temp\` にエクスポートされます。ファイルダイアログは Phase 4 以降で実装予定です。

### Q2: メタデータファイルは必須?

**A**: はい。メタデータファイルは、Next Design の要素 ID と Mermaid ID の対応関係を保持するため必須です。インポート時（Phase 5+）に既存要素を更新するために使用されます。

### Q3: Mermaid ファイルを手動編集できる?

**A**: はい、可能です。ただし、手動編集後は以下に注意:
- メタデータファイルの ID と一致させる
- Mermaid 構文に従う
- インポート時に検証される

### Q4: Phase 4 の実装予定は?

**A**: フラグメント、アクティベーション、ノート対応は Phase 4 で実装予定です。具体的なスケジュールは [developer-guide.md](developer-guide.md) を参照してください。

### Q5: 複数のシーケンス図を一括エクスポートできる?

**A**: 現在のバージョンでは1つずつエクスポートする必要があります。一括エクスポート機能は将来のバージョンで検討されます。

### Q6: エクスポートしたファイルを GitHub で共有できる?

**A**: はい、`.mmd` と `.meta.json` の両方を Git リポジトリにコミットできます。チームメンバーは Mermaid ファイルをプレビューし、必要に応じて Next Design にインポート（Phase 5+）できます。

### Q7: C# スクリプトに `public` が使えない理由は?

**A**: Next Design の C# スクリプト実行環境では、トップレベルのアクセス修飾子がサポートされていません。これは Next Design の仕様です。詳細は [developer-guide.md](developer-guide.md) を参照してください。

### Q8: エラーが解決しない場合は?

**A**: 以下の情報を添えて GitHub Issues に報告してください:
1. Next Design のバージョン
2. エクステンションのバージョン
3. エラーメッセージ（出力ウィンドウのログ全体）
4. 再現手順
5. シーケンス図のスクリーンショット（可能であれば）

## デバッグのヒント

### ログレベルの調整

```csharp
// main.cs で詳細ログを有効化
const bool DEBUG_MODE = true;

if (DEBUG_MODE)
{
    Output.WriteLine("Info", $"[MermaidConverter] DEBUG: {variable}");
}
```

### リフレクションデバッグ

```csharp
// オブジェクトの全プロパティを出力
foreach (var prop in obj.GetType().GetProperties())
{
    try
    {
        var value = prop.GetValue(obj);
        Output.WriteLine("Info", $"[MermaidConverter] {prop.Name}: {value}");
    }
    catch (Exception ex)
    {
        Output.WriteLine("Warning", $"[MermaidConverter] {prop.Name}: エラー");
    }
}
```

### ステップバイステップ実行

```csharp
try
{
    Output.WriteLine("Info", "[MermaidConverter] Step 1: モデル取得");
    var model = CurrentModel;
    
    Output.WriteLine("Info", "[MermaidConverter] Step 2: ライフライン取得");
    var lifelines = GetLifelines(model);
    
    Output.WriteLine("Info", "[MermaidConverter] Step 3: メッセージ取得");
    var messages = GetMessages(model);
    
    Output.WriteLine("Info", "[MermaidConverter] Step 4: ファイル出力");
    SaveFiles(lifelines, messages);
}
catch (Exception ex)
{
    Output.WriteLine("Error", $"[MermaidConverter] エラー発生箇所を特定: {ex.Message}");
}
```

## サポート

### コミュニティ

- GitHub Issues: バグ報告、機能リクエスト
- GitHub Discussions: 質問、議論

### ドキュメント

- [ユーザーガイド](user-guide.md)
- [開発者ガイド](developer-guide.md)
- [ファイル形式](file-format.md)

### 参考資料

- [Next Design ドキュメント](https://docs.nextdesign.app/)
- [Mermaid ドキュメント](https://mermaid.js.org/)
