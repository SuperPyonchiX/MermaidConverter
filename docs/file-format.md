# ファイル形式仕様

NextDesign Mermaid Converter で使用されるファイル形式の詳細仕様です。

## ファイル命名規則

### Mermaid ファイル

- 形式: `{SequenceName}_diagram.mmd`
- 文字コード: UTF-8
- 改行コード: LF または CRLF

例:
- `login_diagram.mmd`
- `checkout_flow_diagram.mmd`

### メタデータファイル

- 形式: `{SequenceName}_diagram.meta.json`
- Mermaid ファイルと同じベース名
- 文字コード: UTF-8

例:
- `login_diagram.meta.json`
- `checkout_flow_diagram.meta.json`

## Mermaid 構文

サポートされる Mermaid シーケンス図の構文:

### 基本構造

エクスポートされる `.mmd` ファイルは、Markdown対応エディタ（GitHub、VS Code等）で即座にプレビューできるよう、Mermaidコードブロックで囲まれています:

```mermaid
sequenceDiagram
    [ライフライン定義]
    [メッセージ定義]
    [フラグメント定義]
    [ノート定義]
```

**インポート時**: Phase 5 実装では、コードブロック（` ```mermaid ` と ` ``` `）は自動的に除去され、本体のみがパースされます。

### ライフライン定義

#### participant (通常のライフライン)

```mermaid
participant LifelineId
participant LifelineId as 表示名
```

例:
```mermaid
participant User
participant UI as ユーザーインターフェース
```

#### actor (アクター) - Phase 4+

```mermaid
actor ActorId
actor ActorId as 表示名
```

例:
```mermaid
actor User
actor Admin as 管理者
```

### メッセージ定義

#### 同期メッセージ

```mermaid
Source->>Target: メッセージ名
```

#### 非同期メッセージ - Phase 4+

```mermaid
Source-->>Target: メッセージ名
```

#### 戻りメッセージ - Phase 4+

```mermaid
Source-->>Target: メッセージ名
Source--)Target: メッセージ名
```

#### Create/Destroy - Phase 4+

```mermaid
Source->>+Target: Create
Source->>xTarget: Destroy
```

### アクティベーション - Phase 4+

```mermaid
Source->>+Target: メッセージ（アクティベーション開始）
Source->>-Target: メッセージ（アクティベーション終了）
```

### フラグメント - Phase 4+

#### alt (条件分岐)

```mermaid
alt 条件1
    A->>B: メッセージ1
else 条件2
    A->>C: メッセージ2
end
```

#### loop (繰り返し)

```mermaid
loop 繰り返し条件
    A->>B: メッセージ
end
```

#### opt (オプショナル)

```mermaid
opt 条件
    A->>B: メッセージ
end
```

#### par (並行)

```mermaid
par 並行処理1
    A->>B: メッセージ1
and 並行処理2
    A->>C: メッセージ2
end
```

### ノート - Phase 4+

```mermaid
Note left of Lifeline: ノート内容
Note right of Lifeline: ノート内容
Note over Lifeline1,Lifeline2: ノート内容
```

## メタデータ JSON 形式

### ルート構造

```json
{
  "version": "1.0",
  "diagram": { ... },
  "lifelines": [ ... ],
  "messages": [ ... ],
  "fragments": [ ... ],
  "unsupportedElements": [ ... ]
}
```

### diagram オブジェクト

シーケンス図全体の情報:

```json
{
  "id": "next-design-guid",
  "name": "シーケンス図名",
  "description": "説明（オプショナル）",
  "customFields": {
    "key": "value"
  }
}
```

| フィールド | 型 | 必須 | 説明 |
|----------|---|------|------|
| `id` | string | ✅ | Next Design の GUID |
| `name` | string | ✅ | シーケンス図名 |
| `description` | string | ❌ | 説明文 |
| `customFields` | object | ❌ | カスタムフィールド（Phase 4+） |

### lifelines 配列

ライフライン要素の配列:

```json
{
  "mermaidId": "User",
  "nextDesignId": "guid-001",
  "name": "ユーザー",
  "type": "Actor",
  "order": 1,
  "customFields": {}
}
```

| フィールド | 型 | 必須 | 説明 |
|----------|---|------|------|
| `mermaidId` | string | ✅ | Mermaid での識別子 |
| `nextDesignId` | string | ✅ | Next Design の GUID |
| `name` | string | ✅ | 表示名 |
| `type` | string | ✅ | "Actor" または "Participant" |
| `order` | number | ✅ | 表示順序（1から開始） |
| `customFields` | object | ❌ | カスタムフィールド |

### messages 配列

メッセージ要素の配列:

```json
{
  "mermaidSourceId": "User",
  "mermaidTargetId": "UI",
  "nextDesignId": "guid-101",
  "name": "ログイン",
  "messageSort": "Synchronous",
  "order": 1,
  "customFields": {}
}
```

| フィールド | 型 | 必須 | 説明 |
|----------|---|------|------|
| `mermaidSourceId` | string | ✅ | Source ライフラインの Mermaid ID |
| `mermaidTargetId` | string | ✅ | Target ライフラインの Mermaid ID |
| `nextDesignId` | string | ✅ | Next Design の GUID |
| `name` | string | ✅ | メッセージ名 |
| `messageSort` | string | ✅ | メッセージ種別（下表参照） |
| `order` | number | ✅ | 表示順序（1から開始） |
| `customFields` | object | ❌ | カスタムフィールド |

#### messageSort 値

| 値 | 説明 | Mermaid 構文 |
|----|------|------------|
| `Synchronous` | 同期メッセージ | `->>` |
| `Asynchronous` | 非同期メッセージ | `-->>` |
| `Return` | 戻りメッセージ | `-->>` または `--)` |
| `Create` | 生成メッセージ | `->>+` |
| `Destroy` | 破棄メッセージ | `->>x` |

### fragments 配列 - Phase 4+

フラグメント要素の配列:

```json
{
  "nextDesignId": "guid-201",
  "type": "alt",
  "guard": "認証成功",
  "order": 5,
  "nestLevel": 0,
  "customFields": {}
}
```

| フィールド | 型 | 必須 | 説明 |
|----------|---|------|------|
| `nextDesignId` | string | ✅ | Next Design の GUID |
| `type` | string | ✅ | "alt", "loop", "opt", "par" |
| `guard` | string | ✅ | ガード条件 |
| `order` | number | ✅ | 表示順序 |
| `nestLevel` | number | ✅ | ネストレベル（0から開始） |
| `customFields` | object | ❌ | カスタムフィールド |

### unsupportedElements 配列

未サポート要素の警告:

```json
[
  "ネストフラグメント（レベル6以上）",
  "スタイリング: rect rgb(200, 220, 100)"
]
```

## バージョン管理

### version フィールド

メタデータ形式のバージョン:

```json
{
  "version": "1.0"
}
```

現在サポートされるバージョン:
- `1.0` - 初期バージョン

将来の互換性:
- メジャーバージョン変更（2.0等）: 破壊的変更
- マイナーバージョン変更（1.1等）: 下位互換性あり

## ID 生成規則

### プレビュー方法

エクスポートされた `.mmd` ファイルは以下の方法で即座にプレビュー可能:

1. **VS Code**: Markdown Preview Mermaid Support 拡張機能をインストール
2. **GitHub/GitLab**: リポジトリにプッシュすると自動レンダリング
3. **Mermaid Live Editor**: https://mermaid.live/ にコピー&ペースト
4. **Obsidian/Notion**: 直接貼り付けでプレビュー

### Mermaid ID

Mermaid ファイル内で使用される識別子:

#### 生成ルール

1. Next Design の名前をベースに生成
2. スペースとハイフン (`-`) をアンダースコア (`_`) に置換
3. 英数字とアンダースコアのみを残す
4. 先頭が数字の場合は `L` を接頭辞として追加

#### 例

| Next Design 名 | Mermaid ID |
|---------------|-----------|
| `User` | `User` |
| `ユーザー UI` | `UI` |
| `認証サービス` | `` |
| `1st Service` | `L1st_Service` |

### Next Design GUID

Next Design の内部 ID:

- 形式: GUID (UUID)
- 例: `a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6`
- 生成タイミング:
  - エクスポート時: 既存の GUID を使用
  - インポート時: メタデータがあれば既存 GUID、なければ新規生成

## エンコーディング

### ファイル文字コード

すべてのファイルは **UTF-8** エンコーディングを使用:

- Mermaid ファイル (`.mmd`): UTF-8
- メタデータ JSON (`.meta.json`): UTF-8

### 改行コード

- **推奨**: LF (`\n`)
- **サポート**: CRLF (`\r\n`)

## 検証ルール

### メタデータ検証

#### 必須フィールド

すべての必須フィールドが存在し、有効な値であること:

- `version`: 有効なバージョン文字列
- `diagram.id`: 空でない文字列
- `diagram.name`: 空でない文字列
- `lifelines[].mermaidId`: 一意の識別子
- `lifelines[].nextDesignId`: 一意の GUID

#### 参照整合性

- `messages[].mermaidSourceId` は `lifelines[].mermaidId` に存在すること
- `messages[].mermaidTargetId` は `lifelines[].mermaidId` に存在すること

### Mermaid 構文検証

Phase 5 で実装予定:

- 構文エラーの検出
- 未定義ライフラインの参照検出
- ネストフラグメントの深さ制限チェック（最大5レベル）

## 拡張性

### カスタムフィールド

各要素に `customFields` オブジェクトを追加可能:

```json
{
  "customFields": {
    "priority": "high",
    "category": "authentication",
    "tags": ["security", "login"]
  }
}
```

- 任意のJSON値をサポート
- Next Design 固有のプロパティを保存
- エクスポート/インポート時に保持

### 将来の拡張

Phase 4+ で追加予定:

- スタイリング情報
- アノテーション
- リンク情報
- カスタムプロパティのマッピング
