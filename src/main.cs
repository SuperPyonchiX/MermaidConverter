// NextDesign Mermaid Converter - C# Script
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NextDesign.Core;
using NextDesign.Desktop;

// コマンドハンドラ: エクスポート
void ExportToMermaid(ICommandContext context, ICommandParams parameters)
{
    try
    {
        Output.WriteLine(OutputLevel.Information, "[MermaidConverter] エクスポート開始");
        
        var model = CurrentModel;
        if (model == null)
        {
            UI.ShowMessageBox("シーケンス図を選択してください。", "エラー");
            return;
        }
        
        // Phase 2-3 の基本実装: 固定パスにエクスポート
        var filePath = @"C:\temp\exported_diagram.mmd";
        var metaPath = @"C:\temp\exported_diagram.meta.json";
        
        // ディレクトリ作成
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        // エクスポート実行
        var content = new StringBuilder();
        content.AppendLine("```mermaid");
        content.AppendLine("sequenceDiagram");
        
        var metadata = new Dictionary<string, object>
        {
            ["version"] = "1.0",
            ["diagram"] = new Dictionary<string, object>(),
            ["lifelines"] = new List<object>(),
            ["messages"] = new List<object>()
        };
        
        var name = "Untitled";
        try
        {
            var prop = model.GetType().GetProperty("Name");
            if (prop != null)
            {
                name = prop.GetValue(model)?.ToString() ?? "Untitled";
            }
        }
        catch { }
        
        ((Dictionary<string, object>)metadata["diagram"])["name"] = name;
        
        Output.WriteLine(OutputLevel.Information, $"[MermaidConverter] シーケンス図: {name}");
        
        // ライフライン取得
        var lifelineCount = 0;
        try
        {
            var llProp = model.GetType().GetProperty("Lifelines");
            if (llProp != null)
            {
                var llValue = llProp.GetValue(model);
                if (llValue is System.Collections.IEnumerable llEnum)
                {
                    var order = 1;
                    foreach (var ll in llEnum)
                    {
                        if (ll == null) continue;
                        
                        var llName = "Lifeline" + order;
                        var llId = Guid.NewGuid().ToString();
                        
                        try
                        {
                            var nameProp = ll.GetType().GetProperty("Name");
                            if (nameProp != null)
                            {
                                llName = nameProp.GetValue(ll)?.ToString() ?? llName;
                            }
                            
                            var idProp = ll.GetType().GetProperty("Id");
                            if (idProp != null)
                            {
                                llId = idProp.GetValue(ll)?.ToString() ?? llId;
                            }
                        }
                        catch { }
                        
                        var mermaidId = Regex.Replace(llName, @"[\s\-]", "_");
                        mermaidId = Regex.Replace(mermaidId, @"[^\w]", "");
                        if (Regex.IsMatch(mermaidId, @"^\d")) mermaidId = "L" + mermaidId;
                        
                        content.AppendLine($"    participant {mermaidId}");
                        
                        ((List<object>)metadata["lifelines"]).Add(new Dictionary<string, object>
                        {
                            ["mermaidId"] = mermaidId,
                            ["nextDesignId"] = llId,
                            ["name"] = llName,
                            ["type"] = "Participant",
                            ["order"] = order
                        });
                        
                        lifelineCount++;
                        order++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Output.WriteLine(OutputLevel.Warning, $"[MermaidConverter] ライフライン取得エラー: {ex.Message}");
        }
        
        Output.WriteLine(OutputLevel.Information, $"[MermaidConverter] ライフライン: {lifelineCount}個");
        
        // メッセージ取得
        var messageCount = 0;
        try
        {
            var msgProp = model.GetType().GetProperty("Messages");
            if (msgProp != null)
            {
                var msgValue = msgProp.GetValue(model);
                if (msgValue is System.Collections.IEnumerable msgEnum)
                {
                    // ライフラインマップ作成
                    var llMap = new Dictionary<string, string>();
                    var lifelinesJson = metadata["lifelines"].ToString();
                    var lifelinesList = JsonSerializer.Deserialize<List<JsonElement>>(lifelinesJson);
                    foreach (var llData in lifelinesList)
                    {
                        var ndId = llData.GetProperty("nextDesignId").GetString();
                        var mId = llData.GetProperty("mermaidId").GetString();
                        llMap[ndId] = mId;
                    }
                    
                    var order = 1;
                    foreach (var msg in msgEnum)
                    {
                        if (msg == null) continue;
                        
                        var msgName = "Message" + order;
                        var msgId = Guid.NewGuid().ToString();
                        string srcId = null;
                        string tgtId = null;
                        
                        try
                        {
                            var nameProp = msg.GetType().GetProperty("Name");
                            if (nameProp != null)
                            {
                                msgName = nameProp.GetValue(msg)?.ToString() ?? msgName;
                            }
                            
                            var idProp = msg.GetType().GetProperty("Id");
                            if (idProp != null)
                            {
                                msgId = idProp.GetValue(msg)?.ToString() ?? msgId;
                            }
                            
                            var srcProp = msg.GetType().GetProperty("Source");
                            if (srcProp != null)
                            {
                                var src = srcProp.GetValue(msg);
                                if (src != null)
                                {
                                    var srcIdProp = src.GetType().GetProperty("Id");
                                    if (srcIdProp != null)
                                    {
                                        srcId = srcIdProp.GetValue(src)?.ToString();
                                    }
                                }
                            }
                            
                            var tgtProp = msg.GetType().GetProperty("Target");
                            if (tgtProp != null)
                            {
                                var tgt = tgtProp.GetValue(msg);
                                if (tgt != null)
                                {
                                    var tgtIdProp = tgt.GetType().GetProperty("Id");
                                    if (tgtIdProp != null)
                                    {
                                        tgtId = tgtIdProp.GetValue(tgt)?.ToString();
                                    }
                                }
                            }
                        }
                        catch { }
                        
                        if (srcId != null && tgtId != null && llMap.ContainsKey(srcId) && llMap.ContainsKey(tgtId))
                        {
                            var srcMermaid = llMap[srcId];
                            var tgtMermaid = llMap[tgtId];
                            
                            content.AppendLine($"    {srcMermaid}->>{tgtMermaid}: {msgName}");
                            
                            ((List<object>)metadata["messages"]).Add(new Dictionary<string, object>
                            {
                                ["mermaidSourceId"] = srcMermaid,
                                ["mermaidTargetId"] = tgtMermaid,
                                ["nextDesignId"] = msgId,
                                ["name"] = msgName,
                                ["messageSort"] = "Synchronous",
                                ["order"] = order
                            });
                            
                            messageCount++;
                        }
                        
                        order++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Output.WriteLine(OutputLevel.Warning, $"[MermaidConverter] メッセージ取得エラー: {ex.Message}");
        }
        
        Output.WriteLine(OutputLevel.Information, $"[MermaidConverter] メッセージ: {messageCount}個");
        
        // Mermaidコードブロック終了
        content.AppendLine("```");
        
        // ファイル書き込み
        File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
        
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var metadataJson = JsonSerializer.Serialize(metadata, jsonOptions);
        File.WriteAllText(metaPath, metadataJson, Encoding.UTF8);
        
        UI.ShowMessageBox(
            $"エクスポート完了\n\nファイル: {filePath}\n\nライフライン: {lifelineCount}\nメッセージ: {messageCount}",
            "成功");
    }
    catch (Exception ex)
    {
        Output.WriteLine(OutputLevel.Error, $"[MermaidConverter] エラー: {ex.Message}\n{ex.StackTrace}");
        UI.ShowMessageBox($"エラーが発生しました:\n\n{ex.Message}", "エラー");
    }
}

// コマンドハンドラ: インポート
void ImportFromMermaid(ICommandContext context, ICommandParams parameters)
{
    // Phase 5 実装予定
    // 
    // インポート時の処理:
    // 1. .mmdファイルを読み込み
    // 2. ```mermaid と ``` で囲まれている場合は除去
    // 3. sequenceDiagram以降の本体をパース
    // 4. メタデータ(.meta.json)があれば読み込み、ID対応付け
    // 5. Next Designモデルを生成/更新
    // 
    // パース例:
    // var content = File.ReadAllText(mmdPath);
    // content = Regex.Replace(content, @"^```mermaid\s*", "");
    // content = Regex.Replace(content, @"\s*```$", "");
    // var lines = content.Split('\n');
    // foreach (var line in lines) { /* パース処理 */ }
    
    UI.ShowMessageBox("インポート機能は Phase 5 で実装予定です。", "未実装");
}
