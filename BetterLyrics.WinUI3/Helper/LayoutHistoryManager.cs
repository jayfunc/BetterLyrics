using BetterLyrics.WinUI3.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Helper
{
    public class LayoutHistoryManager
    {
        private readonly Stack<string?> _undoStack = new();
        private readonly Stack<string?> _redoStack = new();
        private string? _currentStateJson;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            TypeInfoResolver = Serialization.SourceGenerationContext.Default
        };

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void SaveSnapshot(LayoutProfile profile)
        {
            if (profile == null) return;

            string newJson = JsonSerializer.Serialize(profile, _jsonOptions);

            // 防止无意义的连续点击存入相同状态
            if (_currentStateJson != null && _currentStateJson != newJson)
            {
                _undoStack.Push(_currentStateJson);
                _redoStack.Clear(); // 一旦有了新操作，重做栈必须清空
            }

            _currentStateJson = newJson;
        }

        public LayoutProfile? Undo()
        {
            if (!CanUndo) return null;

            _redoStack.Push(_currentStateJson);
            _currentStateJson = _undoStack.Pop();

            return _currentStateJson == null ? null : JsonSerializer.Deserialize<LayoutProfile>(_currentStateJson, _jsonOptions);
        }

        public LayoutProfile? Redo()
        {
            if (!CanRedo) return null;

            _undoStack.Push(_currentStateJson);
            _currentStateJson = _redoStack.Pop();

            return _currentStateJson == null ? null : JsonSerializer.Deserialize<LayoutProfile>(_currentStateJson, _jsonOptions);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentStateJson = null;
        }
    }
}