# Plan: NextDesign - Mermaid シーケンス図完全双方向変換機能の実装 (C#スクリプト版)

NextDesignシーケンス図とMermaid形式の完全な双方向変換を**C#スクリプト(.cs)**で実装します。メタデータは`{SequenceName}_diagram.mmd` + `{SequenceName}_diagram.meta.json`で管理、メタデータなしでも新規ID割り当てでインポート可能、デバッグ環境なしのため防御的プログラミング徹底、全機能を段階的に実装して最終的に完全版をリリースします。

**重要**: 
- Next Designは**C#スクリプト(.cs)のみ**をサポートしており、JavaScriptはサポートされていません
- エントリーポイントは単一の`.cs`ファイルで、manifest.jsonの`main`プロパティで指定します
- すべてのコマンドハンドラは公開関数(`public void HandlerName(ICommandContext context, ICommandParams parameters)`)として実装します
- ファイル分割は可能ですが、すべてのハンドラは1つのエントリーポイントファイルに定義する必要があります

## Steps

1. **プロジェクト基盤構築**: [README.md](README.md)(概要・機能一覧・インストール・使用方法)、[.gitignore](.gitignore)、[LICENSE](LICENSE)、`.editorconfig`を作成、ディレクトリ構造(`src/`, `examples/`, `docs/`)を整備

2. **manifest.jsonと防御的コアフレームワーク**: [manifest.json](manifest.json)でリボンタブとコマンド定義(`main`: "main.cs")、[main.cs](main.cs)ですべてのコマンドハンドラを公開関数として実装(グローバルエラーハンドリング含む)、共通ユーティリティ(ログ・バリデーション・ファイルI/O)も同じファイルに実装、.NETのSystem.IO/System.Text.Json使用

3. **エクスポート機能 - 基本要素**: [main.cs](main.cs)にエクスポート関連のヘルパー関数を追加、`ISequenceDiagram`走査→ライフライン(participant/actor判定)→メッセージ(同期`->>`/非同期`--)`/戻り`-->>`/Create/Destroy)変換、System.Text.Jsonを使用してID・順序・カスタムフィールドを`{name}_diagram.meta.json`に保存

4. **エクスポート機能 - 高度な要素**: [main.cs](main.cs)のエクスポート機能を拡張してalt/loop/opt/parフラグメント変換(ネスト対応)、アクティベーション(`+`/`-`)処理、ノート(`Note left/right/over`)対応、未対応要素はメタデータ保存+警告リスト作成

5. **インポート機能 - 完全実装**: [main.cs](main.cs)にインポート関連のヘルパー関数を追加、全構文パース(Regex使用、ネストフラグメント用Stack)、`App.Workspace.BeginTransaction()`→メタデータ自動検出→Next Design APIでモデル構築→`CommitTransaction()`、エラー時`RollbackTransaction()`+詳細エラーダイアログ

6. **テスト・サンプル・ドキュメント完全整備**: [examples/login_diagram.mmd](examples/login_diagram.mmd)+`.meta.json`(基本例)、[examples/complex_flow_diagram.mmd](examples/complex_flow_diagram.mmd)(フラグメント・ネスト含む)、[docs/user-guide.md](docs/user-guide.md)、[docs/developer-guide.md](docs/developer-guide.md)、[docs/file-format.md](docs/file-format.md)(メタデータ仕様)、[docs/troubleshooting.md](docs/troubleshooting.md)、変換結果サマリーダイアログ実装

## Further Considerations

1. **Next Design APIの未確認要素**: `ISequenceDiagram`、`ILifeline`、`IMessage`、`IInteractionFragment`の実際のプロパティ名やメソッドがドキュメントと異なる可能性があるため、実装時に段階的に確認しながら進める必要があるが、それでも問題ないか?

2. **エラーリカバリー戦略**: 大規模図のインポート途中でエラーが発生した場合、部分的にインポート済みの要素をロールバックで完全に削除するか、エラー箇所以降をスキップして部分インポート結果を残す選択肢をユーザーに提示するか?

3. **パフォーマンス実測のタイミング**: 実装完了後、100要素・500要素・1000要素の図でパフォーマンステストを行い、問題があれば最適化(非同期処理・バッチ処理・プログレスバー)を追加実装する流れで良いか?

## 技術仕様詳細

### ファイル命名規則
- Mermaidファイル: `{SequenceName}_diagram.mmd`
- メタデータファイル: `{SequenceName}_diagram.meta.json`
- 例: `login_diagram.mmd` + `login_diagram.meta.json`

### メタデータ構造 (`.meta.json`)
```json
{
  "version": "1.0",
  "diagram": {
    "id": "diagram_guid",
    "name": "Login Sequence",
    "description": "ユーザーログインフロー",
    "customFields": {}
  },
  "lifelines": [
    {
      "mermaidId": "User",
      "nextDesignId": "lifeline_guid_001",
      "name": "ユーザー",
      "type": "Actor",
      "order": 1,
      "customFields": {}
    }
  ],
  "messages": [
    {
      "mermaidSourceId": "User",
      "mermaidTargetId": "UI",
      "nextDesignId": "message_guid_001",
      "name": "ログイン",
      "messageSort": "Synchronous",
      "order": 1,
      "customFields": {}
    }
  ],
  "fragments": [
    {
      "nextDesignId": "fragment_guid_001",
      "type": "alt",
      "guard": "認証成功",
      "order": 5,
      "nestLevel": 0
    }
  ],
  "unsupportedElements": []
}
```

### Next Design API 想定マッピング
| Next Design要素 | プロパティ (想定) | Mermaid構文 |
|----------------|------------------|-------------|
| `ISequenceDiagram` | `.Name`, `.Lifelines`, `.Messages`, `.Fragments` | `sequenceDiagram` |
| `ILifeline` | `.Name`, `.Type`, `.Order` | `participant`/`actor` |
| `IMessage` | `.Name`, `.Source`, `.Target`, `.MessageSort`, `.Order` | `->>`, `-->>`, `--)` |
| `IInteractionFragment` | `.Type`, `.Guard`, `.Operands` | `alt`, `loop`, `opt`, `par` |

### エラーハンドリング戦略
```csharp
// 防御的プログラミングパターン
public static T SafeGetProperty<T>(object obj, string propertyName, T defaultValue = default(T))
{
    try
    {
        if (obj == null) return defaultValue;
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop == null) return defaultValue;
        var value = prop.GetValue(obj);
        return value != null ? (T)value : defaultValue;
    }
    catch (Exception ex)
    {
        Output.WriteLine(OutputLevel.Warning, $"プロパティアクセスエラー: {propertyName} - {ex.Message}");
        return defaultValue;
    }
}

// トランザクション管理
public static bool ExecuteWithTransaction(string operationName, Action operation)
{
    App.Workspace.BeginTransaction(operationName);
    try
    {
        operation();
        App.Workspace.CommitTransaction();
        return true;
    }
    catch (Exception ex)
    {
        App.Workspace.RollbackTransaction();
        Output.WriteLine(OutputLevel.Error, $"{operationName}失敗: {ex.Message}");
        UI.ShowMessageBox($"{operationName}中にエラーが発生しました:\n\n{ex.Message}", "エラー");
        return false;
    }
}
```

### 段階的実装アプローチ
1. **Phase 1**: manifest.json + 基本フレームワーク (エラーハンドリング・ログ・バリデーション)
2. **Phase 2**: エクスポート基本機能 (ライフライン・基本メッセージ)
3. **Phase 3**: インポート基本機能 (パーサー・基本要素生成)
4. **Phase 4**: エクスポート高度機能 (フラグメント・アクティベーション・ノート)
5. **Phase 5**: インポート高度機能 (フラグメントパース・複雑構造)
6. **Phase 6**: 統合テスト・ドキュメント・サンプル

### サポート要素一覧
#### 完全サポート
- ✅ Lifeline (participant/actor)
- ✅ Message (同期・非同期・戻り・Create・Destroy)
- ✅ Activation (`+`/`-`)
- ✅ Fragment (alt/loop/opt/par)
- ✅ Note (left/right/over)
- ✅ カスタムフィールド (メタデータ経由)

#### 部分サポート (メタデータ保存)
- ⚠️ ネストフラグメント (最大5レベル、超過時は警告)
- ⚠️ 複雑なガード条件 (テキストとして保存)
- ⚠️ Next Design固有の拡張プロパティ

#### 未サポート (警告表示)
- ❌ Mermaid固有のスタイリング (rect/背景色)
- ❌ autonumber (番号自動割り当て)
- ❌ links (外部リンク)

### ユーザーインターフェース
#### エクスポートフロー
1. Next Designでシーケンス図を選択
2. リボンタブ「Mermaid変換」→「Mermaidへエクスポート」クリック
3. 保存ダイアログでファイル名入力 (自動的に`_diagram.mmd`接尾辞追加)
4. エクスポート実行→進捗表示→完了ダイアログ (警告があれば詳細表示)
5. `{name}_diagram.mmd` + `{name}_diagram.meta.json`が生成される

#### インポートフロー
1. Next Designでプロジェクトまたは親要素を選択
2. リボンタブ「Mermaid変換」→「Mermaidからインポート」クリック
3. 開くダイアログで`.mmd`ファイル選択
4. 同名の`.meta.json`を自動検出 (なければ新規ID生成モード)
5. インポート実行→トランザクション内で要素生成→完了ダイアログ

#### エラーダイアログ例
```
インポート中にエラーが発生しました

ファイル: login_diagram.mmd
エラー箇所: 行 42
エラー内容: 未定義のライフライン 'Unknown' が参照されています

処理: ロールバックしました (変更は適用されていません)

詳細ログ: [ログファイルを開く]
```

### 開発ガイドライン
- **命名規則**: PascalCase (クラス・メソッド)、camelCase (ローカル変数)、C#標準に準拠
- **コメント**: XML Documentation Comments形式で関数・クラスに説明
- **エラー処理**: すべてのNext Design API呼び出しに`try-catch`
- **ログ出力**: `Output.WriteLine(LogLevel.*, message)` 使用
- **.NET API**: System.IO、System.Text.Json、System.Text.RegularExpressions等を活用

### C#スクリプト実装の注意点
- **エントリーポイント**: 単一の`.cs`ファイル(manifest.jsonの`main`プロパティで指定)
- **コマンドハンドラ**: 公開関数として実装 `public void HandlerName(ICommandContext context, ICommandParams parameters)`
- **execFunc**: manifest.jsonのコマンド定義でハンドラ関数名を指定
- **usingディレクティブ**: 必要な名前空間をインポート(NextDesign.Core, NextDesign.Desktop, System.IO等)
- **グローバルオブジェクト**: App, Context, UI, CurrentModel等が直接アクセス可能
- **JSON処理**: System.Text.Jsonを使用 (Newtonsoft.Jsonは不可)

### 配布パッケージ構成
```
MermaidConverter.zip
├── manifest.json
├── main.cs           # 単一エントリーポイント(すべてのハンドラとヘルパー関数)
├── examples/
│   ├── login_diagram.mmd
│   ├── login_diagram.meta.json
│   └── complex_flow_diagram.mmd
├── docs/
│   ├── user-guide.md
│   └── troubleshooting.md
├── LICENSE
└── README.md
```
