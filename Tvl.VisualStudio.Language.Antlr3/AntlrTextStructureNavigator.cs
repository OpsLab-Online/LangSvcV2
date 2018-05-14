﻿namespace Tvl.VisualStudio.Language.Antlr3
{
    using JetBrains.Annotations;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Utilities;

    internal class AntlrTextStructureNavigator : ITextStructureNavigator
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ITextStructureNavigator _delegateNavigator;

        public AntlrTextStructureNavigator([NotNull] ITextBuffer textBuffer, [NotNull] ITextStructureNavigator delegateNavigator)
        {
            Requires.NotNull(textBuffer, nameof(textBuffer));
            Requires.NotNull(delegateNavigator, nameof(delegateNavigator));

            _textBuffer = textBuffer;
            _delegateNavigator = delegateNavigator;
        }

        public ITextBuffer TextBuffer
        {
            get
            {
                return _textBuffer;
            }
        }

        public IContentType ContentType
        {
            get
            {
                return _textBuffer.ContentType;
            }
        }

        public TextExtent GetExtentOfWord(SnapshotPoint currentPosition)
        {
            TextExtent extent = _delegateNavigator.GetExtentOfWord(currentPosition);
            if (extent.IsSignificant)
            {
                var span = extent.Span;
                if (span.Start > 0 && IsIdentifierStartChar(span.Start.GetChar()) && IsIdentifierStartChar((span.Start - 1).GetChar()))
                    extent = new TextExtent(new SnapshotSpan(span.Start - 1, span.End), true);
            }

            return extent;
        }

        private bool IsIdentifierStartChar(char c)
        {
            if (char.IsLetter(c))
                return true;

            if (c == '_' || c == '$' || c == '@')
                return true;

            return false;
        }

        public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan)
        {
            return _delegateNavigator.GetSpanOfEnclosing(activeSpan);
        }

        public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan)
        {
            return _delegateNavigator.GetSpanOfFirstChild(activeSpan);
        }

        public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan)
        {
            return _delegateNavigator.GetSpanOfNextSibling(activeSpan);
        }

        public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan)
        {
            return _delegateNavigator.GetSpanOfPreviousSibling(activeSpan);
        }
    }
}
