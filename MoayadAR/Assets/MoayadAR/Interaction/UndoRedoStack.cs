using System;
using System.Collections.Generic;
using MoayadAR.Core;

namespace MoayadAR.Interaction
{
    public interface IReversibleCommand
    {
        string Label { get; }     // diagnostics label
        void Redo();
        void Undo();
    }

    /// <summary>Transform change on a placed model. Holds both poses; applying sets absolute values (no drift accumulation).</summary>
    public sealed class SetPoseCommand : IReversibleCommand
    {
        private readonly Action<TransformPose> _apply;
        private readonly TransformPose _before, _after;
        public string Label { get; }
        public SetPoseCommand(string label, TransformPose before, TransformPose after, Action<TransformPose> apply)
        { Label = label; _before = before; _after = after; _apply = apply; }
        public void Redo() => _apply(_after);
        public void Undo() => _apply(_before);
    }

    /// <summary>
    /// Multi-step Undo/Redo. A new command clears the redo branch (standard editor semantics).
    /// Depth-capped so memory stays bounded during long editing sessions.
    /// </summary>
    public sealed class UndoRedoStack
    {
        private readonly LinkedList<IReversibleCommand> _done = new LinkedList<IReversibleCommand>();
        private readonly Stack<IReversibleCommand> _undone = new Stack<IReversibleCommand>();
        private readonly int _maxDepth;

        public UndoRedoStack(int maxDepth = 100) { _maxDepth = Math.Max(1, maxDepth); }

        public int UndoCount => _done.Count;
        public int RedoCount => _undone.Count;
        public bool CanUndo => _done.Count > 0;
        public bool CanRedo => _undone.Count > 0;

        public void Push(IReversibleCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            cmd.Redo();
            _done.AddLast(cmd);
            while (_done.Count > _maxDepth) _done.RemoveFirst();
            _undone.Clear();
        }

        public bool Undo()
        {
            if (_done.Count == 0) return false;
            var cmd = _done.Last.Value;
            _done.RemoveLast();
            cmd.Undo();
            _undone.Push(cmd);
            return true;
        }

        public bool Redo()
        {
            if (_undone.Count == 0) return false;
            var cmd = _undone.Pop();
            cmd.Redo();
            _done.AddLast(cmd);
            return true;
        }

        public void Clear() { _done.Clear(); _undone.Clear(); }
    }
}
