using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Helper
{
    public class LayoutHistoryManager
    {
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private string? _currentStateJson;

        // JSON 序列化配置（解决之前遇到的 NaN/Infinity 问题，并允许循环引用如果存在的话）
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // 记录新的快照
        public void SaveSnapshot(LayoutProfile profile)
        {
            if (profile == null) return;

            string newJson = JsonSerializer.Serialize(profile, _jsonOptions);

            // 只有当状态真正发生改变时才记录（防止无意义的连续点击存入相同状态）
            if (_currentStateJson != null && _currentStateJson != newJson)
            {
                _undoStack.Push(_currentStateJson);
                _redoStack.Clear(); // 一旦有了新操作，重做栈必须清空
            }

            _currentStateJson = newJson;
        }

        // 撤销
        public LayoutProfile? Undo()
        {
            if (!CanUndo) return null;

            _redoStack.Push(_currentStateJson); // 把当前状态压入重做栈
            _currentStateJson = _undoStack.Pop(); // 取出上一个状态

            return JsonSerializer.Deserialize<LayoutProfile>(_currentStateJson, _jsonOptions);
        }

        // 重做
        public LayoutProfile Redo()
        {
            if (!CanRedo) return null;

            _undoStack.Push(_currentStateJson); // 把当前状态压入撤销栈
            _currentStateJson = _redoStack.Pop(); // 取出下一个状态

            return JsonSerializer.Deserialize<LayoutProfile>(_currentStateJson, _jsonOptions);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentStateJson = null;
        }
    }
}