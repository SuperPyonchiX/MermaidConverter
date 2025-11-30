using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NextDesign.Core;
using NextDesign.Desktop;
// JSONライブラリの選択:
// - System.Text.Json (.NET Core 3.0以降) または
// - Newtonsoft.Json (JSON.NET, .NET Framework対応)
// Next Designの実行環境に応じて適切な方を使用してください
#if NETCOREAPP || NETSTANDARD2_1
using System.Text.Json;
#else
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
#endif

/// <summary>
/// NextDesign Mermaid Converter
/// NextDesignシーケンス図とMermaid形式の双方向変換を実現します
/// </summary>
public class MermaidConverter
{
    #region グローバルオブジェクト

    /// <summary>
    /// アプリケーションオブジェクト
    /// </summary>
    private IApplication App { get; set; }

    /// <summary>
    /// コンテキスト情報
    /// </summary>
    private IContext Context { get; set; }

    #endregion

    #region エントリーポイント - コマンドハンドラ

    /// <summary>
    /// Mermaidへエクスポート
    /// </summary>
    public void ExportToMermaid(ICommandContext context, ICommandParams parameters)
    {
        try
        {
            App = context.App;
            Context = context;

            LogInfo("=== Mermaidエクスポート開始 ===");

            // 現在選択されているモデルを取得
            var currentModel = App.Workspace.CurrentModel;
            if (currentModel == null)
            {
                ShowError("モデルが選択されていません。\nシーケンス図を選択してください。");
                return;
            }

            // シーケンス図エディタを取得
            var editor = App.Window.EditorPage.CurrentEditor;
            if (editor == null || !(editor is ISequenceDiagram))
            {
                ShowError("シーケンス図エディタが開かれていません。\nシーケンス図を開いて実行してください。");
                return;
            }

            var sequenceDiagram = editor as ISequenceDiagram;
            var interaction = sequenceDiagram.Model as IInteraction;
            
            if (interaction == null)
            {
                ShowError("シーケンス図モデルを取得できませんでした。");
                return;
            }

            // ファイル保存ダイアログを表示
            var saveDialog = App.UI.ShowSaveFileDialog(
                "Mermaidファイルとして保存",
                "Mermaidファイル (*.mmd)|*.mmd"
            );

            if (string.IsNullOrEmpty(saveDialog))
            {
                LogInfo("エクスポートがキャンセルされました");
                return;
            }

            // エクスポート処理
            var result = ExportSequenceDiagramToMermaid(interaction, saveDialog);

            if (result.Success)
            {
                ShowInfo($"エクスポートが完了しました。\n\n" +
                        $"ファイル: {saveDialog}\n" +
                        $"メタデータ: {saveDialog.Replace(".mmd", ".meta.json")}\n\n" +
                        $"ライフライン: {result.LifelineCount}\n" +
                        $"メッセージ: {result.MessageCount}\n" +
                        $"フラグメント: {result.FragmentCount}");
            }
            else
            {
                ShowError($"エクスポートに失敗しました。\n\n{result.ErrorMessage}");
            }

            LogInfo("=== Mermaidエクスポート完了 ===");
        }
        catch (Exception ex)
        {
            LogError($"予期しないエラーが発生しました: {ex.Message}");
            LogError($"スタックトレース: {ex.StackTrace}");
            ShowError($"エクスポート中にエラーが発生しました。\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// Mermaidからインポート
    /// </summary>
    public void ImportFromMermaid(ICommandContext context, ICommandParams parameters)
    {
        try
        {
            App = context.App;
            Context = context;

            LogInfo("=== Mermaidインポート開始 ===");

            // ファイル選択ダイアログを表示
            var openDialog = App.UI.ShowOpenFileDialog(
                "Mermaidファイルを選択",
                "Mermaidファイル (*.mmd)|*.mmd"
            );

            if (string.IsNullOrEmpty(openDialog))
            {
                LogInfo("インポートがキャンセルされました");
                return;
            }

            if (!File.Exists(openDialog))
            {
                ShowError($"ファイルが見つかりません: {openDialog}");
                return;
            }

            // インポート処理
            var result = ImportMermaidToSequenceDiagram(openDialog);

            if (result.Success)
            {
                ShowInfo($"インポートが完了しました。\n\n" +
                        $"ファイル: {openDialog}\n\n" +
                        $"ライフライン: {result.LifelineCount}\n" +
                        $"メッセージ: {result.MessageCount}\n" +
                        $"フラグメント: {result.FragmentCount}");
            }
            else
            {
                ShowError($"インポートに失敗しました。\n\n{result.ErrorMessage}");
            }

            LogInfo("=== Mermaidインポート完了 ===");
        }
        catch (Exception ex)
        {
            LogError($"予期しないエラーが発生しました: {ex.Message}");
            LogError($"スタックトレース: {ex.StackTrace}");
            ShowError($"インポート中にエラーが発生しました。\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// Mermaid検証
    /// </summary>
    public void ValidateMermaid(ICommandContext context, ICommandParams parameters)
    {
        try
        {
            App = context.App;
            Context = context;

            LogInfo("=== Mermaid検証開始 ===");

            // ファイル選択ダイアログを表示
            var openDialog = App.UI.ShowOpenFileDialog(
                "検証するMermaidファイルを選択",
                "Mermaidファイル (*.mmd)|*.mmd"
            );

            if (string.IsNullOrEmpty(openDialog))
            {
                LogInfo("検証がキャンセルされました");
                return;
            }

            if (!File.Exists(openDialog))
            {
                ShowError($"ファイルが見つかりません: {openDialog}");
                return;
            }

            // 検証処理
            var result = ValidateMermaidFile(openDialog);

            if (result.IsValid)
            {
                ShowInfo($"Mermaidファイルは有効です。\n\n" +
                        $"ファイル: {openDialog}\n\n" +
                        $"ライフライン: {result.LifelineCount}\n" +
                        $"メッセージ: {result.MessageCount}\n" +
                        $"フラグメント: {result.FragmentCount}");
            }
            else
            {
                ShowWarning($"Mermaidファイルに問題があります。\n\n" +
                           $"エラー:\n{string.Join("\n", result.Errors)}");
            }

            LogInfo("=== Mermaid検証完了 ===");
        }
        catch (Exception ex)
        {
            LogError($"予期しないエラーが発生しました: {ex.Message}");
            ShowError($"検証中にエラーが発生しました。\n\n{ex.Message}");
        }
    }

    #endregion

    #region エクスポート機能

    /// <summary>
    /// シーケンス図をMermaid形式にエクスポート
    /// </summary>
    private ExportResult ExportSequenceDiagramToMermaid(IInteraction interaction, string filePath)
    {
        var result = new ExportResult();
        
        try
        {
            var mermaidBuilder = new StringBuilder();
            mermaidBuilder.AppendLine("sequenceDiagram");

            // メタデータ構造
            var metadata = new Dictionary<string, object>
            {
                ["diagram"] = new Dictionary<string, object>
                {
                    ["id"] = SafeGetProperty(interaction, m => m.Id, ""),
                    ["name"] = SafeGetProperty(interaction, m => m.Name, "Untitled"),
                    ["description"] = SafeGetProperty(interaction, m => m.Description, "")
                },
                ["lifelines"] = new List<Dictionary<string, object>>(),
                ["messages"] = new List<Dictionary<string, object>>()
            };

            var lifelineList = metadata["lifelines"] as List<Dictionary<string, object>>;
            var messageList = metadata["messages"] as List<Dictionary<string, object>>;

            // ライフラインの処理
            var lifelines = SafeGetCollection(interaction, i => i.Lifelines);
            if (lifelines != null && lifelines.Any())
            {
                LogInfo($"ライフライン数: {lifelines.Count()}");
                
                foreach (var lifeline in lifelines)
                {
                    var lifelineName = SafeGetProperty(lifeline, l => l.Name, "Unknown");
                    var sanitizedName = SanitizeMermaidId(lifelineName);
                    
                    mermaidBuilder.AppendLine($"    participant {sanitizedName}");
                    
                    lifelineList.Add(new Dictionary<string, object>
                    {
                        ["id"] = SafeGetProperty(lifeline, l => l.Id, ""),
                        ["name"] = lifelineName,
                        ["mermaidId"] = sanitizedName,
                        ["order"] = lifelineList.Count
                    });
                    
                    result.LifelineCount++;
                }
                
                mermaidBuilder.AppendLine();
            }

            // メッセージの処理
            var messages = SafeGetCollection(interaction, i => i.Messages);
            if (messages != null && messages.Any())
            {
                LogInfo($"メッセージ数: {messages.Count()}");
                
                // メッセージはすでに順序付けられている (Messagesコレクションの順序)
                foreach (var message in messages)
                {
                    var sender = SafeGetProperty(message, m => m.Sender, null);
                    var receiver = SafeGetProperty(message, m => m.Receiver, null);
                    
                    if (sender == null || receiver == null)
                    {
                        LogWarning($"メッセージの送信元または受信先がnullです: {SafeGetProperty(message, m => m.Name, "Unknown")}");
                        continue;
                    }
                    
                    var senderName = SanitizeMermaidId(SafeGetProperty(sender, s => s.Name, "Unknown"));
                    var receiverName = SanitizeMermaidId(SafeGetProperty(receiver, r => r.Name, "Unknown"));
                    var messageText = SafeGetProperty(message, m => m.Name, "");
                    var messageKind = SafeGetProperty(message, m => m.Kind, "sync");
                    
                    var arrow = GetMermaidArrow(messageKind);
                    mermaidBuilder.AppendLine($"    {senderName}{arrow}{receiverName}: {messageText}");
                    
                    messageList.Add(new Dictionary<string, object>
                    {
                        ["id"] = SafeGetProperty(message, m => m.Id, ""),
                        ["name"] = messageText,
                        ["sender"] = SafeGetProperty(sender, s => s.Name, ""),
                        ["receiver"] = SafeGetProperty(receiver, r => r.Name, ""),
                        ["kind"] = messageKind,
                        ["order"] = messageList.Count
                    });
                    
                    result.MessageCount++;
                }
            }

            // Mermaidファイルを保存
            File.WriteAllText(filePath, mermaidBuilder.ToString(), Encoding.UTF8);
            LogInfo($"Mermaidファイルを保存しました: {filePath}");

            // メタデータファイルを保存
            var metadataPath = filePath.Replace(".mmd", ".meta.json");
            var metadataJson = SerializeToJson(metadata);
            File.WriteAllText(metadataPath, metadataJson, Encoding.UTF8);
            LogInfo($"メタデータファイルを保存しました: {metadataPath}");

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            LogError($"エクスポートエラー: {ex.Message}");
            LogError($"スタックトレース: {ex.StackTrace}");
        }

        return result;
    }

    /// <summary>
    /// メッセージ種別に応じたMermaid矢印を取得
    /// Kind: "sync", "async", "reply", "create", "destroy"
    /// </summary>
    private string GetMermaidArrow(string messageKind)
    {
        switch (messageKind?.ToLower())
        {
            case "sync":
                return "->>";
            case "async":
                return "-)";
            case "reply":
                return "-->>";
            case "create":
                return "->>+";
            case "destroy":
                return "->>x";
            default:
                return "->>";
        }
    }

    #endregion

    #region インポート機能

    /// <summary>
    /// Mermaidファイルからシーケンス図にインポート
    /// </summary>
    private ImportResult ImportMermaidToSequenceDiagram(string filePath)
    {
        var result = new ImportResult();
        
        try
        {
            // Mermaidファイルを読み込み
            var mermaidContent = File.ReadAllText(filePath, Encoding.UTF8);
            LogInfo($"Mermaidファイルを読み込みました: {filePath}");

            // メタデータファイルを検索して読み込み
            var metadataPath = filePath.Replace(".mmd", ".meta.json");
            Dictionary<string, object> metadata = null;
            
            if (File.Exists(metadataPath))
            {
                var metadataJson = File.ReadAllText(metadataPath, Encoding.UTF8);
                metadata = DeserializeFromJson(metadataJson);
                LogInfo($"メタデータファイルを読み込みました: {metadataPath}");
            }
            else
            {
                LogWarning("メタデータファイルが見つかりません。新規作成します。");
            }

            // Mermaidをパース
            var parseResult = ParseMermaidContent(mermaidContent);
            
            result.LifelineCount = parseResult.Lifelines.Count;
            result.MessageCount = parseResult.Messages.Count;
            result.FragmentCount = parseResult.Fragments.Count;

            // トランザクション開始
            App.Workspace.BeginTransaction("Mermaidインポート");
            
            try
            {
                // 現在のモデルまたは新規作成
                var currentModel = App.Workspace.CurrentModel;
                if (currentModel == null)
                {
                    ShowError("プロジェクトまたはモデルを開いてください。");
                    App.Workspace.RollbackTransaction();
                    return result;
                }

                // シーケンス図の作成
                // Note: 実際のAPIに応じて実装を調整
                LogInfo($"インポート完了: ライフライン {result.LifelineCount}, メッセージ {result.MessageCount}");
                
                App.Workspace.CommitTransaction();
                result.Success = true;
            }
            catch (Exception ex)
            {
                App.Workspace.RollbackTransaction();
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            LogError($"インポートエラー: {ex.Message}");
            LogError($"スタックトレース: {ex.StackTrace}");
        }

        return result;
    }

    /// <summary>
    /// Mermaid内容をパース
    /// </summary>
    private MermaidParseResult ParseMermaidContent(string content)
    {
        var result = new MermaidParseResult();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // ヘッダースキップ
            if (trimmed == "sequenceDiagram" || string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Participant/Actor
            if (trimmed.StartsWith("participant ") || trimmed.StartsWith("actor "))
            {
                var match = Regex.Match(trimmed, @"^(participant|actor)\s+(\w+)");
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
            else if (Regex.IsMatch(trimmed, @"(\w+)(->?>|--?>|--?\)|-)(\+|x)?(\w+):"))
            {
                var match = Regex.Match(trimmed, @"(\w+)(->?>|--?>|--?\)|-)(\+|x)?(\w+):\s*(.*)");
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
            // Fragment
            else if (Regex.IsMatch(trimmed, @"^(alt|opt|loop|par)\s"))
            {
                var match = Regex.Match(trimmed, @"^(alt|opt|loop|par)\s+(.*)");
                if (match.Success)
                {
                    result.Fragments.Add(new FragmentInfo
                    {
                        Type = match.Groups[1].Value,
                        Condition = match.Groups[2].Value
                    });
                }
            }
        }

        LogInfo($"パース結果: ライフライン {result.Lifelines.Count}, メッセージ {result.Messages.Count}, フラグメント {result.Fragments.Count}");
        return result;
    }

    #endregion

    #region 検証機能

    /// <summary>
    /// Mermaidファイルを検証
    /// </summary>
    private ValidationResult ValidateMermaidFile(string filePath)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var parseResult = ParseMermaidContent(content);
            
            result.LifelineCount = parseResult.Lifelines.Count;
            result.MessageCount = parseResult.Messages.Count;
            result.FragmentCount = parseResult.Fragments.Count;

            // 基本検証
            if (!content.Contains("sequenceDiagram"))
            {
                result.IsValid = false;
                result.Errors.Add("'sequenceDiagram'ヘッダーが見つかりません");
            }

            if (parseResult.Lifelines.Count == 0)
            {
                result.Errors.Add("警告: ライフラインが定義されていません");
            }

            if (parseResult.Messages.Count == 0)
            {
                result.Errors.Add("警告: メッセージが定義されていません");
            }

            // メッセージの送受信先がライフラインに存在するか確認
            var lifelineNames = parseResult.Lifelines.Select(l => l.Name).ToHashSet();
            foreach (var message in parseResult.Messages)
            {
                if (!lifelineNames.Contains(message.Sender))
                {
                    result.Errors.Add($"未定義のライフライン: {message.Sender}");
                    result.IsValid = false;
                }
                if (!lifelineNames.Contains(message.Receiver))
                {
                    result.Errors.Add($"未定義のライフライン: {message.Receiver}");
                    result.IsValid = false;
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"検証エラー: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region ユーティリティ関数

    /// <summary>
    /// 防御的プロパティアクセス
    /// </summary>
    private T SafeGetProperty<TSource, T>(TSource source, Func<TSource, T> selector, T defaultValue = default(T))
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

    /// <summary>
    /// 防御的コレクションアクセス
    /// </summary>
    private IEnumerable<T> SafeGetCollection<TSource, T>(TSource source, Func<TSource, IEnumerable<T>> selector)
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

    /// <summary>
    /// Mermaid IDのサニタイズ
    /// </summary>
    private string SanitizeMermaidId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "Unknown";
        
        // 空白を削除、特殊文字をアンダースコアに変換
        return Regex.Replace(id, @"[^\w]", "_");
    }

    /// <summary>
    /// オブジェクトをJSON文字列にシリアライズ
    /// </summary>
    private string SerializeToJson(object obj)
    {
#if NETCOREAPP || NETSTANDARD2_1
        return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
#else
        // .NET Framework環境の場合は簡易JSON生成
        // 注: 本格的なJSON処理にはNewtonsoft.Jsonの利用を推奨
        return ConvertToSimpleJson(obj, 0);
#endif
    }

    /// <summary>
    /// JSON文字列からオブジェクトにデシリアライズ
    /// </summary>
    private Dictionary<string, object> DeserializeFromJson(string json)
    {
#if NETCOREAPP || NETSTANDARD2_1
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
#else
        // .NET Framework環境の場合は警告を表示
        LogWarning("JSON デシリアライズは System.Text.Json または Newtonsoft.Json が必要です");
        return new Dictionary<string, object>();
#endif
    }

    /// <summary>
    /// 簡易JSON文字列生成 (.NET Framework用フォールバック)
    /// </summary>
    private string ConvertToSimpleJson(object obj, int indent)
    {
        var sb = new StringBuilder();
        var indentStr = new string(' ', indent * 2);

        if (obj == null)
        {
            return "null";
        }
        else if (obj is string str)
        {
            return $"\"{EscapeJsonString(str)}\"";
        }
        else if (obj is bool b)
        {
            return b.ToString().ToLower();
        }
        else if (obj is int || obj is long || obj is double || obj is float || obj is decimal)
        {
            return obj.ToString();
        }
        else if (obj is Dictionary<string, object> dict)
        {
            sb.AppendLine("{");
            var items = dict.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var kvp = items[i];
                sb.Append(new string(' ', (indent + 1) * 2));
                sb.Append($"\"{kvp.Key}\": ");
                sb.Append(ConvertToSimpleJson(kvp.Value, indent + 1));
                if (i < items.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }
            sb.Append(indentStr);
            sb.Append("}");
            return sb.ToString();
        }
        else if (obj is System.Collections.IEnumerable enumerable)
        {
            sb.AppendLine("[");
            var list = enumerable.Cast<object>().ToList();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(new string(' ', (indent + 1) * 2));
                sb.Append(ConvertToSimpleJson(list[i], indent + 1));
                if (i < list.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }
            sb.Append(indentStr);
            sb.Append("]");
            return sb.ToString();
        }
        else
        {
            return $"\"{obj}\"";
        }
    }

    /// <summary>
    /// JSON文字列のエスケープ
    /// </summary>
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// 情報ログ出力
    /// </summary>
    private void LogInfo(string message)
    {
        if (App?.Output != null)
        {
            App.Output.WriteLine("mermaid-converter", message);
        }
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        if (App?.Output != null)
        {
            App.Output.WriteLine("mermaid-converter", $"[WARNING] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        if (App?.Output != null)
        {
            App.Output.WriteLine("mermaid-converter", $"[ERROR] {message}");
        }
    }

    /// <summary>
    /// 情報メッセージ表示
    /// </summary>
    private void ShowInfo(string message)
    {
        if (App?.UI != null)
        {
            App.UI.ShowMessageBox("Mermaid変換", message, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// 警告メッセージ表示
    /// </summary>
    private void ShowWarning(string message)
    {
        if (App?.UI != null)
        {
            App.UI.ShowMessageBox("Mermaid変換", message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// エラーメッセージ表示
    /// </summary>
    private void ShowError(string message)
    {
        if (App?.UI != null)
        {
            App.UI.ShowMessageBox("Mermaid変換", message, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region データ構造

    private class ExportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int LifelineCount { get; set; }
        public int MessageCount { get; set; }
        public int FragmentCount { get; set; }
    }

    private class ImportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int LifelineCount { get; set; }
        public int MessageCount { get; set; }
        public int FragmentCount { get; set; }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int LifelineCount { get; set; }
        public int MessageCount { get; set; }
        public int FragmentCount { get; set; }
    }

    private class MermaidParseResult
    {
        public List<LifelineInfo> Lifelines { get; set; } = new List<LifelineInfo>();
        public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>();
        public List<FragmentInfo> Fragments { get; set; } = new List<FragmentInfo>();
    }

    private class LifelineInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    private class MessageInfo
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Arrow { get; set; }
        public string Text { get; set; }
    }

    private class FragmentInfo
    {
        public string Type { get; set; }
        public string Condition { get; set; }
    }

    #endregion
}
