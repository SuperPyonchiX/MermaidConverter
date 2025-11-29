# NextDesign Mermaid Converter - 開発者ガイド

このガイドでは、NextDesign Mermaid Converter の内部構造と開発方法を説明します。

## 目次

1. [アーキテクチャ](#アーキテクチャ)
2. [開発環境](#開発環境)
3. [フェーズ別実装](#フェーズ別実装)
4. [API リファレンス](#apiリファレンス)
5. [デバッグ](#デバッグ)
6. [テスト](#テスト)

## アーキテクチャ

### ファイル構成

```
MermaidConverter/
├── manifest.json           # エクステンション定義
├── src/
│   └── main.cs            # メインエントリーポイント
├── resources/
│   ├── export.png         # エクスポートアイコン
│   └── import.png         # インポートアイコン
├── examples/
│   ├── *.mmd              # サンプル Mermaid ファイル
│   └── *.meta.json        # サンプルメタデータ
└── docs/
    └── *.md               # ドキュメント
```

### manifest.json 構造

```json
{
  "name": "com.example.MermaidConverter",
  "displayName": "Mermaid Converter",
  "version": "1.0.0",
  "main": "src/main.cs",
  "lifecycle": "application",
  "extensions": [
    {
      "id": "RibbonTab",
      "point": "ribbonTab",
      "label": "Mermaid変換"
    },
    {
      "id": "ExportCommand",
      "point": "command",
      "label": "Mermaidへエクスポート",
      "execFunc": "ExportToMermaid",
      "icon": "resources/export.png"
    }
  ]
}
```

### main.cs エントリーポイント

Next Design C# スクリプトの制約:

1. **アクセス修飾子禁止**: `public`/`private` 不可
2. **ハンドラシグネチャ**: `void HandlerName(ICommandContext, ICommandParams)`
3. **グローバルオブジェクト**: `App`, `Context`, `UI`, `Output`, `CurrentModel`, `Workspace`

```csharp
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using NextDesign.Core;
using NextDesign.Desktop;

// ハンドラ定義（アクセス修飾子なし）
void ExportToMermaid(ICommandContext context, ICommandParams parameters)
{
    try
    {
        // 処理
    }
    catch (Exception ex)
    {
        UI.ShowMessageBox($"エラー: {ex.Message}");
    }
}
```

## 開発環境

### 必須ツール

- **Next Design**: 最新版
- **テキストエディタ**: VS Code または任意のエディタ
- **Git**: バージョン管理

### 推奨設定

#### .editorconfig

```ini
root = true

[*]
charset = utf-8
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_size = 4

[*.json]
indent_size = 2
```

### 開発ワークフロー

1. **編集**: テキストエディタでコード編集
2. **保存**: UTF-8 で保存
3. **再起動**: Next Design を再起動（ホットリロード非対応）
4. **テスト**: 機能をテスト
5. **ログ確認**: 出力ウィンドウでログ確認

## フェーズ別実装

### Phase 1: プロジェクト基盤 ✅

**目的**: プロジェクト構造とドキュメントの整備

**成果物**:
- [x] README.md
- [x] .gitignore
- [x] LICENSE
- [x] .editorconfig
- [x] ディレクトリ構造

**期間**: 完了

### Phase 2: コアフレームワーク ✅

**目的**: エクステンションの骨格構築

**実装**:
- [x] manifest.json
- [x] リボンタブ定義
- [x] コマンド定義
- [x] エントリーポイント作成

**キー実装**:

```csharp
// 基本ハンドラ構造
void ExportToMermaid(ICommandContext context, ICommandParams parameters)
{
    Output.WriteLine("Info", "[MermaidConverter] === Mermaid エクスポート開始 ===");
    
    try
    {
        var model = CurrentModel;
        if (model == null)
        {
            UI.ShowMessageBox("モデルが選択されていません。");
            return;
        }
        
        // 処理本体
    }
    catch (Exception ex)
    {
        Output.WriteLine("Error", $"[MermaidConverter] エラー: {ex.Message}");
        UI.ShowMessageBox($"エクスポート中にエラーが発生しました: {ex.Message}");
    }
}
```

### Phase 3: 基本エクスポート ✅

**目的**: ライフラインとメッセージのエクスポート

**実装**:
- [x] ライフライン抽出
- [x] メッセージ抽出
- [x] Mermaid 構文生成
- [x] メタデータ JSON 生成

**キー実装**:

#### リフレクションベース プロパティアクセス

```csharp
string GetPropertyValue(object obj, string propertyName)
{
    var prop = obj.GetType().GetProperty(propertyName);
    if (prop != null)
    {
        var value = prop.GetValue(obj);
        return value?.ToString() ?? "";
    }
    return "";
}
```

#### ライフライン抽出

```csharp
var lifelinesProperty = model.GetType().GetProperty("Lifelines");
if (lifelinesProperty != null)
{
    var lifelines = lifelinesProperty.GetValue(model) as System.Collections.IEnumerable;
    if (lifelines != null)
    {
        foreach (var lifeline in lifelines)
        {
            string name = GetPropertyValue(lifeline, "Name");
            string id = GetPropertyValue(lifeline, "Id");
            
            mermaidContent.AppendLine($"    participant {SanitizeId(name)}");
            
            // メタデータ保存
            lifelinesMetadata.Add(new Dictionary<string, object>
            {
                { "mermaidId", SanitizeId(name) },
                { "nextDesignId", id },
                { "name", name }
            });
        }
    }
}
```

#### ID サニタイゼーション

```csharp
string SanitizeId(string name)
{
    if (string.IsNullOrEmpty(name)) return "Unknown";
    
    var sanitized = new StringBuilder();
    foreach (char c in name)
    {
        if (char.IsLetterOrDigit(c) || c == '_')
        {
            sanitized.Append(c);
        }
        else if (c == ' ' || c == '-')
        {
            sanitized.Append('_');
        }
    }
    
    string result = sanitized.ToString();
    if (string.IsNullOrEmpty(result)) return "Unknown";
    if (char.IsDigit(result[0])) result = "L" + result;
    
    return result;
}
```

#### メッセージ抽出

```csharp
var messagesProperty = model.GetType().GetProperty("Messages");
if (messagesProperty != null)
{
    var messages = messagesProperty.GetValue(model) as System.Collections.IEnumerable;
    if (messages != null)
    {
        foreach (var message in messages)
        {
            var sourceProp = message.GetType().GetProperty("Source");
            var targetProp = message.GetType().GetProperty("Target");
            
            if (sourceProp != null && targetProp != null)
            {
                var source = sourceProp.GetValue(message);
                var target = targetProp.GetValue(message);
                
                if (source != null && target != null)
                {
                    string sourceName = GetPropertyValue(source, "Name");
                    string targetName = GetPropertyValue(target, "Name");
                    string messageName = GetPropertyValue(message, "Name");
                    
                    string sourceId = SanitizeId(sourceName);
                    string targetId = SanitizeId(targetName);
                    
                    mermaidContent.AppendLine($"    {sourceId}->>{targetId}: {messageName}");
                }
            }
        }
    }
}
```

### Phase 4: 高度なエクスポート ⚠️ 未実装

**目的**: フラグメント、アクティベーション、ノート対応

**実装予定**:
- [ ] alt/loop/opt/par フラグメント
- [ ] アクティベーション (+/-)
- [ ] ノート (Note left/right/over)
- [ ] アクター (actor)
- [ ] 非同期メッセージ (-->>)

**実装ガイド**:

#### フラグメント抽出

```csharp
// Phase 4 実装例
void ExportFragments(object model, StringBuilder mermaidContent, List<object> fragmentsMetadata)
{
    var fragmentsProperty = model.GetType().GetProperty("Fragments");
    if (fragmentsProperty != null)
    {
        var fragments = fragmentsProperty.GetValue(model) as System.Collections.IEnumerable;
        if (fragments != null)
        {
            foreach (var fragment in fragments)
            {
                string type = GetPropertyValue(fragment, "InteractionOperator");
                string guard = GetPropertyValue(fragment, "Guard");
                
                // Mermaid 構文生成
                switch (type.ToLower())
                {
                    case "alt":
                        mermaidContent.AppendLine($"    alt {guard}");
                        // ネスト要素処理
                        mermaidContent.AppendLine("    end");
                        break;
                    case "loop":
                        mermaidContent.AppendLine($"    loop {guard}");
                        mermaidContent.AppendLine("    end");
                        break;
                    // 他のタイプ...
                }
                
                // メタデータ保存
                fragmentsMetadata.Add(new Dictionary<string, object>
                {
                    { "nextDesignId", GetPropertyValue(fragment, "Id") },
                    { "type", type },
                    { "guard", guard }
                });
            }
        }
    }
}
```

#### アクティベーション

```csharp
// Phase 4 実装例
void ExportActivations(object message, StringBuilder mermaidContent)
{
    // アクティベーション開始
    bool activationStart = GetPropertyValue(message, "ActivationStart") == "True";
    bool activationEnd = GetPropertyValue(message, "ActivationEnd") == "True";
    
    string arrow = "->>";
    if (activationStart) arrow = "->>+";
    if (activationEnd) arrow = "->>-";
    
    mermaidContent.AppendLine($"    {sourceId}{arrow}{targetId}: {messageName}");
}
```

### Phase 5: インポート完全実装 ⚠️ 未実装

**目的**: Mermaid から Next Design へのフルインポート

**実装予定**:
- [ ] Mermaid 構文パーサー
- [ ] メタデータ自動検出
- [ ] Next Design モデル生成
- [ ] トランザクション管理
- [ ] エラーハンドリング

**実装ガイド**:

#### Mermaid パーサー

```csharp
// Phase 5 実装例
Dictionary<string, object> ParseMermaid(string mermaidContent)
{
    var result = new Dictionary<string, object>();
    var lifelines = new List<Dictionary<string, string>>();
    var messages = new List<Dictionary<string, string>>();
    
    var lines = mermaidContent.Split('\n');
    foreach (var line in lines)
    {
        var trimmed = line.Trim();
        
        // participant 解析
        if (trimmed.StartsWith("participant "))
        {
            var parts = trimmed.Substring(12).Split(new[] { " as " }, StringSplitOptions.None);
            lifelines.Add(new Dictionary<string, string>
            {
                { "mermaidId", parts[0].Trim() },
                { "name", parts.Length > 1 ? parts[1].Trim() : parts[0].Trim() }
            });
        }
        
        // メッセージ解析
        else if (trimmed.Contains("->>"))
        {
            var messageParts = trimmed.Split(new[] { "->>" }, StringSplitOptions.None);
            if (messageParts.Length == 2)
            {
                var targetAndName = messageParts[1].Split(':');
                messages.Add(new Dictionary<string, string>
                {
                    { "source", messageParts[0].Trim() },
                    { "target", targetAndName[0].Trim() },
                    { "name", targetAndName.Length > 1 ? targetAndName[1].Trim() : "" }
                });
            }
        }
    }
    
    result["lifelines"] = lifelines;
    result["messages"] = messages;
    return result;
}
```

#### モデル生成

```csharp
// Phase 5 実装例
void CreateNextDesignModel(string diagramName, Dictionary<string, object> parsedData)
{
    try
    {
        // トランザクション開始
        App.Workspace.BeginTransaction();
        
        // シーケンス図作成
        var sequenceDiagram = App.Workspace.CreateModel("SequenceDiagram");
        sequenceDiagram.SetField("Name", diagramName);
        
        // ライフライン作成
        var lifelines = parsedData["lifelines"] as List<Dictionary<string, string>>;
        foreach (var lifelineData in lifelines)
        {
            var lifeline = sequenceDiagram.AddChild("Lifelines", "Lifeline");
            lifeline.SetField("Name", lifelineData["name"]);
        }
        
        // メッセージ作成
        var messages = parsedData["messages"] as List<Dictionary<string, string>>;
        foreach (var messageData in messages)
        {
            var message = sequenceDiagram.AddChild("Messages", "Message");
            message.SetField("Name", messageData["name"]);
            // Source/Target 設定...
        }
        
        // トランザクション完了
        App.Workspace.CommitTransaction();
        
        UI.ShowMessageBox("インポートが完了しました。");
    }
    catch (Exception ex)
    {
        // ロールバック
        App.Workspace.RollbackTransaction();
        throw;
    }
}
```

### Phase 6: 最終仕上げ ⚠️ 未実装

**目的**: 完全なドキュメント、テスト、サンプル

**実装予定**:
- [x] user-guide.md
- [x] file-format.md
- [ ] developer-guide.md (本ドキュメント)
- [ ] troubleshooting.md
- [ ] 複雑なサンプル追加

## API リファレンス

### グローバルオブジェクト

Next Design で利用可能なグローバルオブジェクト:

#### App

```csharp
// アプリケーションインスタンス
IApplication App

// ワークスペースアクセス
App.Workspace.BeginTransaction();
App.Workspace.CommitTransaction();
App.Workspace.RollbackTransaction();

// モデル作成
var model = App.Workspace.CreateModel("ModelClassName");
```

#### Context

```csharp
// コマンドコンテキスト
ICommandContext Context

// パラメータアクセス
string value = Context.Parameters.GetParameterValue("paramName");
```

#### UI

```csharp
// UIサービス
IUIService UI

// メッセージボックス
UI.ShowMessageBox("メッセージ");
UI.ShowMessageBox("メッセージ", "タイトル");

// ファイルダイアログ
string filePath = UI.ShowSaveFileDialog("保存", "Mermaid Files (*.mmd)|*.mmd");
string filePath = UI.ShowOpenFileDialog("開く", "Mermaid Files (*.mmd)|*.mmd");
```

#### Output

```csharp
// 出力ウィンドウ
IOutputService Output

// ログ出力
Output.WriteLine("Info", "[MermaidConverter] 情報メッセージ");
Output.WriteLine("Warning", "[MermaidConverter] 警告");
Output.WriteLine("Error", "[MermaidConverter] エラー");
```

#### CurrentModel

```csharp
// 現在選択されているモデル
IModel CurrentModel

// プロパティアクセス（リフレクション推奨）
string name = CurrentModel.GetType().GetProperty("Name").GetValue(CurrentModel) as string;
```

### リフレクション API

```csharp
// プロパティ存在チェック
var prop = obj.GetType().GetProperty("PropertyName");
if (prop != null)
{
    var value = prop.GetValue(obj);
}

// メソッド呼び出し
var method = obj.GetType().GetMethod("MethodName");
if (method != null)
{
    var result = method.Invoke(obj, new object[] { arg1, arg2 });
}

// コレクション列挙
var collection = obj as System.Collections.IEnumerable;
if (collection != null)
{
    foreach (var item in collection)
    {
        // 処理
    }
}
```

## デバッグ

### ログ出力

```csharp
Output.WriteLine("Info", $"[MermaidConverter] 変数: {variable}");
Output.WriteLine("Info", $"[MermaidConverter] オブジェクト型: {obj.GetType().FullName}");
```

### エラーハンドリング

```csharp
void ExportToMermaid(ICommandContext context, ICommandParams parameters)
{
    try
    {
        // 処理
        Output.WriteLine("Info", "[MermaidConverter] 処理開始");
        
        // ... 処理本体 ...
        
        Output.WriteLine("Info", "[MermaidConverter] 処理完了");
    }
    catch (Exception ex)
    {
        Output.WriteLine("Error", $"[MermaidConverter] エラー: {ex.Message}");
        Output.WriteLine("Error", $"[MermaidConverter] スタックトレース: {ex.StackTrace}");
        UI.ShowMessageBox($"エラーが発生しました:\n{ex.Message}");
    }
}
```

### リフレクションデバッグ

```csharp
// オブジェクトの全プロパティを出力
void DebugPrintProperties(object obj, string prefix = "")
{
    if (obj == null)
    {
        Output.WriteLine("Info", $"{prefix}null");
        return;
    }
    
    var type = obj.GetType();
    Output.WriteLine("Info", $"{prefix}Type: {type.FullName}");
    
    foreach (var prop in type.GetProperties())
    {
        try
        {
            var value = prop.GetValue(obj);
            Output.WriteLine("Info", $"{prefix}{prop.Name}: {value}");
        }
        catch (Exception ex)
        {
            Output.WriteLine("Warning", $"{prefix}{prop.Name}: エラー - {ex.Message}");
        }
    }
}
```

## テスト

### 手動テスト

1. **準備**:
   - Next Design でテストプロジェクトを開く
   - シーケンス図を作成
   - ライフラインとメッセージを追加

2. **エクスポートテスト**:
   - シーケンス図を選択
   - リボン「Mermaid変換」→「Mermaidへエクスポート」
   - 出力ファイルを確認

3. **検証**:
   - `.mmd` ファイルが有効な Mermaid 構文か
   - `.meta.json` が有効な JSON か
   - メタデータの ID が一致しているか

### テストケース

#### 基本ケース

- 2 ライフライン、1 メッセージ
- 4 ライフライン、6 メッセージ
- 日本語名のライフライン

#### エッジケース

- ライフライン0個
- メッセージ0個
- Source または Target が null のメッセージ
- 特殊文字を含む名前

#### Phase 4+ テストケース

- ネストフラグメント
- アクティベーション
- ノート

## ベストプラクティス

### 防御的プログラミング

```csharp
// 常に null チェック
if (obj != null)
{
    var prop = obj.GetType().GetProperty("Name");
    if (prop != null)
    {
        var value = prop.GetValue(obj);
        if (value != null)
        {
            // 処理
        }
    }
}

// コレクション列挙の安全な方法
var collection = obj as System.Collections.IEnumerable;
if (collection != null)
{
    foreach (var item in collection)
    {
        if (item != null)
        {
            // 処理
        }
    }
}
```

### エラーメッセージ

```csharp
// ユーザーフレンドリーなメッセージ
UI.ShowMessageBox("エクスポートが完了しました。");

// 詳細ログ
Output.WriteLine("Info", "[MermaidConverter] ライフライン数: 4, メッセージ数: 6");
Output.WriteLine("Error", $"[MermaidConverter] エラー詳細: {ex.ToString()}");
```

### パフォーマンス

```csharp
// StringBuilder 使用
var sb = new StringBuilder();
sb.AppendLine("sequenceDiagram");
// ... 多数の追加 ...
string result = sb.ToString();

// リフレクション結果のキャッシュ
var cachedProperties = new Dictionary<Type, PropertyInfo[]>();
```

## リリース

### バージョン管理

- manifest.json の version を更新
- CHANGELOG.md を更新
- Git タグを作成

### 配布

1. 不要なファイルを削除
2. ZIP ファイルに圧縮
3. GitHub Releases にアップロード

## 参考資料

- [Next Design エクステンション開発マニュアル](https://docs.nextdesign.app/extension/)
- [Next Design API リファレンス](https://docs.nextdesign.app/extension/api/intro)
- [Mermaid ドキュメント](https://mermaid.js.org/)
