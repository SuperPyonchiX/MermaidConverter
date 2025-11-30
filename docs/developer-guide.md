# 開発者ガイド

NextDesign Mermaid Converterの内部構造と拡張方法を説明します。

## 目次

1. [アーキテクチャ概要](#アーキテクチャ概要)
2. [主要コンポーネント](#主要コンポーネント)
3. [API仕様](#api仕様)
4. [拡張方法](#拡張方法)
5. [デバッグ方法](#デバッグ方法)
6. [テスト戦略](#テスト戦略)

## アーキテクチャ概要

### 全体構成

```
MermaidConverter
├── manifest.json          # エクステンション定義
├── main.cs                # メイン実装
└── resources/             # リソースファイル

処理フロー:
1. ユーザー操作 (リボンボタン)
2. コマンドハンドラ呼び出し
3. Next Design API アクセス
4. ファイルI/O処理
5. 結果表示
```

### 設計原則

1. **防御的プログラミング**
   - すべてのAPI呼び出しをtry-catchで保護
   - nullチェックを徹底
   - 詳細なログ出力

2. **トランザクション管理**
   - モデル変更は必ずトランザクション内で実行
   - エラー時は確実にロールバック

3. **分離原則**
   - UI層とビジネスロジック層を分離
   - ファイルI/Oとモデル操作を分離

4. **拡張性**
   - 新しいMermaid要素の追加が容易
   - カスタムメタデータの追加が容易

## 主要コンポーネント

### 1. manifest.json

エクステンションの定義ファイル。

```json
{
  "name": "MermaidConverter",
  "version": "1.0.0",
  "main": "main.cs",
  "lifecycle": "application",
  "extensionPoints": {
    "ribbon": { ... },
    "commands": [ ... ]
  }
}
```

**重要なポイント:**
- `main`: エントリーポイントのC#ファイル
- `lifecycle`: "application" (アプリケーション起動時に有効化)
- `ribbon.tabs`: リボンタブとグループの定義
- `commands`: コマンドIDと実行関数のマッピング

### 2. main.cs - コマンドハンドラ

```csharp
public void ExportToMermaid(ICommandContext context, ICommandParams parameters)
{
    App = context.App;
    Context = context;
    
    // 処理
}
```

**シグネチャ:**
- `public void` で定義 (必須)
- 第1引数: `ICommandContext` - コンテキスト情報
- 第2引数: `ICommandParams` - パラメータ

**アクセス可能な主要オブジェクト:**
- `context.App` - アプリケーションオブジェクト
- `App.Workspace` - ワークスペース
- `App.Window` - ウィンドウ
- `App.UI` - UIユーティリティ
- `App.Output` - 出力ウィンドウ

### 3. エクスポート機能

#### 処理フロー

```
1. シーケンス図の取得
   ↓
2. ライフラインの走査とMermaid変換
   ↓
3. メッセージの走査とMermaid変換
   ↓
4. フラグメントの走査とMermaid変換 (将来)
   ↓
5. Mermaidファイルの保存
   ↓
6. メタデータファイルの保存
```

#### 主要メソッド

**ExportSequenceDiagramToMermaid**
```csharp
private ExportResult ExportSequenceDiagramToMermaid(
    IInteraction interaction, 
    string filePath)
{
    // 1. Mermaid構文の生成
    var mermaidBuilder = new StringBuilder();
    mermaidBuilder.AppendLine("sequenceDiagram");
    
    // 2. ライフライン処理
    foreach (var lifeline in interaction.Lifelines)
    {
        var sanitizedName = SanitizeMermaidId(lifeline.Name);
        mermaidBuilder.AppendLine($"    participant {sanitizedName}");
    }
    
    // 3. メッセージ処理 (Messagesコレクションの順序を使用)
    var messages = interaction.Messages;
    
    foreach (var message in messages)
    {
        var arrow = GetMermaidArrow(message.Kind);
        mermaidBuilder.AppendLine(
            $"    {sender}{arrow}{receiver}: {message.Name}");
    }
    
    // 4. ファイル保存
    File.WriteAllText(filePath, mermaidBuilder.ToString(), Encoding.UTF8);
    
    // 5. メタデータ保存
    SaveMetadata(filePath, interaction);
    
    return result;
}
```

**GetMermaidArrow**
```csharp
private string GetMermaidArrow(string messageSort)
{
    switch (messageSort?.ToLower())
    {
        case "synccall":
        case "sync":
            return "->>";
        case "asynccall":
        case "async":
            return "-)";
        case "reply":
        case "return":
            return "-->>";
        case "create":
            return "->>+";
        case "destroy":
            return "->>x";
        default:
            return "->>";
    }
}
```

### 4. インポート機能

#### 処理フロー

```
1. Mermaidファイルの読み込み
   ↓
2. メタデータファイルの検出・読み込み
   ↓
3. Mermaid構文のパース
   ↓
4. トランザクション開始
   ↓
5. Next Designモデルの構築
   ↓
6. トランザクションコミット
```

#### 主要メソッド

**ImportMermaidToSequenceDiagram**
```csharp
private ImportResult ImportMermaidToSequenceDiagram(string filePath)
{
    // 1. ファイル読み込み
    var mermaidContent = File.ReadAllText(filePath, Encoding.UTF8);
    
    // 2. メタデータ検出
    var metadataPath = filePath.Replace(".mmd", ".meta.json");
    Dictionary<string, object> metadata = null;
    if (File.Exists(metadataPath))
    {
        var json = File.ReadAllText(metadataPath, Encoding.UTF8);
        metadata = JsonSerializer.Deserialize<...>(json);
    }
    
    // 3. パース
    var parseResult = ParseMermaidContent(mermaidContent);
    
    // 4. トランザクション管理
    App.Workspace.BeginTransaction("Mermaidインポート");
    try
    {
        // モデル構築
        CreateSequenceDiagram(parseResult, metadata);
        
        App.Workspace.CommitTransaction();
    }
    catch
    {
        App.Workspace.RollbackTransaction();
        throw;
    }
    
    return result;
}
```

**ParseMermaidContent**
```csharp
private MermaidParseResult ParseMermaidContent(string content)
{
    var result = new MermaidParseResult();
    var lines = content.Split(new[] { '\r', '\n' }, 
        StringSplitOptions.RemoveEmptyEntries);
    
    foreach (var line in lines)
    {
        var trimmed = line.Trim();
        
        // Participant/Actor
        if (trimmed.StartsWith("participant ") || 
            trimmed.StartsWith("actor "))
        {
            var match = Regex.Match(trimmed, 
                @"^(participant|actor)\s+(\w+)");
            if (match.Success)
            {
                result.Lifelines.Add(new LifelineInfo
                {
                    Name = match.Groups[2].Value,
                    Type = match.Groups[1].Value
                });
            }
        }
        
        // Message
        else if (Regex.IsMatch(trimmed, 
            @"(\w+)(->?>|--?>|--?\)|-)(\+|x)?(\w+):"))
        {
            var match = Regex.Match(trimmed, 
                @"(\w+)(->?>|--?>|--?\)|-)(\+|x)?(\w+):\s*(.*)");
            if (match.Success)
            {
                result.Messages.Add(new MessageInfo
                {
                    Sender = match.Groups[1].Value,
                    Receiver = match.Groups[4].Value,
                    Arrow = match.Groups[2].Value + match.Groups[3].Value,
                    Text = match.Groups[5].Value
                });
            }
        }
    }
    
    return result;
}
```

### 5. ユーティリティ関数

#### 防御的プロパティアクセス

```csharp
private T SafeGetProperty<TSource, T>(
    TSource source, 
    Func<TSource, T> selector, 
    T defaultValue = default(T))
{
    try
    {
        if (source == null)
            return defaultValue;
        return selector(source);
    }
    catch (Exception ex)
    {
        LogWarning($"プロパティアクセスエラー: {ex.Message}");
        return defaultValue;
    }
}
```

**使用例:**
```csharp
var name = SafeGetProperty(lifeline, l => l.Name, "Unknown");
var y = SafeGetProperty(message, m => m.SourceY, 0.0);
```

#### 防御的コレクションアクセス

```csharp
private IEnumerable<T> SafeGetCollection<TSource, T>(
    TSource source, 
    Func<TSource, IEnumerable<T>> selector)
{
    try
    {
        if (source == null)
            return Enumerable.Empty<T>();
        var result = selector(source);
        return result ?? Enumerable.Empty<T>();
    }
    catch (Exception ex)
    {
        LogWarning($"コレクションアクセスエラー: {ex.Message}");
        return Enumerable.Empty<T>();
    }
}
```

**使用例:**
```csharp
var lifelines = SafeGetCollection(interaction, i => i.Lifelines);
var messages = SafeGetCollection(interaction, i => i.Messages);
```

#### Mermaid IDサニタイズ

```csharp
private string SanitizeMermaidId(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        return "Unknown";
    
    // 空白を削除、特殊文字をアンダースコアに変換
    return Regex.Replace(id, @"[^\w]", "_");
}
```

**例:**
- "User Service" → "User_Service"
- "認証サーバー" → "認証サーバー" (日本語はそのまま)
- "API Gateway" → "API_Gateway"

## API仕様

### Next Design API

**ISequenceDiagram**

```csharp
// シーケンス図エディタ
var editor = App.Window.EditorPage.CurrentEditor as ISequenceDiagram;
var interaction = editor.Model as IInteraction;
```

#### IInteraction

```csharp
// シーケンス図モデル
interface IInteraction
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    IEnumerable<ILifeline> Lifelines { get; }
    IEnumerable<IMessage> Messages { get; }
    IEnumerable<IInteractionFragment> Fragments { get; }
}
```

#### ILifeline

```csharp
// ライフライン
interface ILifeline
{
    string Id { get; }
    string Name { get; }
    IModel TypeModel { get; }
    IEnumerable<IExecutionSpecification> ExecutionSpecifications { get; }
}
```

#### IMessage

```csharp
interface IMessage
{
    string Id { get; }
    string Name { get; }
    ILifeline Sender { get; }
    ILifeline Receiver { get; }
    string Kind { get; } // sync, async, create, destroy, reply
    bool IsSynchronous { get; }
    bool IsAsynchronous { get; }
    bool IsReply { get; }
}
```

#### IInteractionFragment

```csharp
// 複合フラグメント
interface IInteractionFragment
{
    string Id { get; }
    string InteractionOperator { get; } // alt, opt, loop, par
    string Guard { get; }
    IEnumerable<IInteractionOperand> Operands { get; }
}
```

### トランザクション管理

```csharp
// トランザクション開始
App.Workspace.BeginTransaction("操作名");

try
{
    // モデル変更処理
    
    // コミット
    App.Workspace.CommitTransaction();
}
catch (Exception ex)
{
    // ロールバック
    App.Workspace.RollbackTransaction();
    throw;
}
```

## 拡張方法

### 1. 新しいMermaid要素のサポート追加

#### ステップ1: パース処理の追加

```csharp
// ParseMermaidContent メソッドに追加
else if (trimmed.StartsWith("Note "))
{
    var match = Regex.Match(trimmed, 
        @"Note (left of|right of|over) (\w+):\s*(.*)");
    if (match.Success)
    {
        result.Notes.Add(new NoteInfo
        {
            Position = match.Groups[1].Value,
            Target = match.Groups[2].Value,
            Text = match.Groups[3].Value
        });
    }
}
```

#### ステップ2: エクスポート処理の追加

```csharp
// ExportSequenceDiagramToMermaid メソッドに追加
// ノートの処理
var notes = SafeGetCollection(interaction, i => i.Notes);
foreach (var note in notes)
{
    var target = SanitizeMermaidId(note.Target);
    mermaidBuilder.AppendLine(
        $"    Note {note.Position} {target}: {note.Text}");
}
```

#### ステップ3: インポート処理の追加

```csharp
// CreateSequenceDiagram メソッドに追加
// ノートの作成
foreach (var noteInfo in parseResult.Notes)
{
    // Next Design APIでノートを作成
    var note = interaction.AddNote(...);
}
```

### 2. カスタムメタデータの追加

#### メタデータ構造の拡張

```csharp
// メタデータにカスタムフィールドを追加
var metadata = new Dictionary<string, object>
{
    ["diagram"] = new Dictionary<string, object>
    {
        ["id"] = interaction.Id,
        ["name"] = interaction.Name,
        // カスタムフィールド
        ["customData"] = new Dictionary<string, object>
        {
            ["author"] = "Your Name",
            ["version"] = "1.0",
            ["tags"] = new[] { "authentication", "api" }
        }
    }
};
```

### 3. 新しいコマンドの追加

#### manifest.jsonに追加

```json
{
  "commands": [
    {
      "id": "MermaidConverter.BatchExport",
      "name": "一括エクスポート",
      "execFunc": "BatchExportToMermaid"
    }
  ]
}
```

#### main.csにハンドラを追加

```csharp
public void BatchExportToMermaid(
    ICommandContext context, 
    ICommandParams parameters)
{
    App = context.App;
    Context = context;
    
    // 一括エクスポート処理
}
```

## デバッグ方法

### 1. ログ出力

```csharp
LogInfo("処理開始");
LogWarning("警告メッセージ");
LogError("エラーメッセージ");
```

**出力先:** Next Design出力ウィンドウ (チャンネル: mermaid-converter)

### 2. デバッグログの追加

```csharp
LogInfo($"Lifeline: {lifeline.Name}, ID: {lifeline.Id}");
LogInfo($"Message: {message.Name}, Sort: {message.MessageSort}");
```

### 3. トラブルシューティング

**エラー発生時の確認事項:**
1. 出力ウィンドウのログを確認
2. スタックトレースを確認
3. APIドキュメントで仕様を確認
4. トランザクション状態を確認

## テスト戦略

### 1. 単体テスト

**テスト対象:**
- `SanitizeMermaidId`: ID変換ロジック
- `GetMermaidArrow`: 矢印変換ロジック
- `ParseMermaidContent`: パース処理

**テスト方法:**
- 独立した関数として切り出し
- 各種入力パターンでテスト

### 2. 統合テスト

**テストシナリオ:**
1. エクスポート → ファイル確認
2. インポート → モデル確認
3. エクスポート → インポート → 整合性確認

**テストデータ:**
- 基本要素のみ
- 高度な要素を含む
- 異常系 (不正な構文)

### 3. パフォーマンステスト

**測定項目:**
- 大量のライフライン (50+)
- 大量のメッセージ (500+)
- ネストフラグメント (5レベル)

## 参考資料

- [Next Design API リファレンス](https://docs.nextdesign.app/extension/api/)
- [Next Design サンプル](https://github.com/denso-create/NextDesign-Samples)
- [Mermaid公式ドキュメント](https://mermaid.js.org/)

## コントリビューション

拡張機能の改善にご協力いただける場合:
1. GitHubでFork
2. 機能ブランチを作成
3. 変更を実装
4. テストを実行
5. Pull Requestを作成
