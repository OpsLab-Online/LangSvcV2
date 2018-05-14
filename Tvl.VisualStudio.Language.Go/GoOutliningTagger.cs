﻿namespace Tvl.VisualStudio.Language.Go
{
    using System;
    using System.Collections.Generic;
    using Antlr.Runtime;
    using Antlr.Runtime.Tree;
    using JetBrains.Annotations;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Tvl.VisualStudio.Language.Parsing;

    internal sealed class GoOutliningTagger : ITagger<IOutliningRegionTag>
    {
        private List<ITagSpan<IOutliningRegionTag>> _outliningRegions;
        private readonly GoOutliningTaggerProvider _provider;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public GoOutliningTagger([NotNull] ITextBuffer textBuffer, [NotNull] IBackgroundParser backgroundParser, [NotNull] GoOutliningTaggerProvider provider)
        {
            Requires.NotNull(textBuffer, nameof(textBuffer));
            Requires.NotNull(backgroundParser, nameof(backgroundParser));
            Requires.NotNull(provider, nameof(provider));

            this.TextBuffer = textBuffer;
            this.BackgroundParser = backgroundParser;
            this._provider = provider;

            this._outliningRegions = new List<ITagSpan<IOutliningRegionTag>>();

            this.BackgroundParser.ParseComplete += HandleBackgroundParseComplete;
            this.BackgroundParser.RequestParse(false);
        }

        private ITextBuffer TextBuffer
        {
            get;
            set;
        }

        private IBackgroundParser BackgroundParser
        {
            get;
            set;
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _outliningRegions;
        }

        private void OnTagsChanged(SnapshotSpanEventArgs e)
        {
            var t = TagsChanged;
            if (t != null)
                t(this, e);
        }

        private void HandleBackgroundParseComplete(object sender, ParseResultEventArgs e)
        {
            AntlrParseResultEventArgs antlrParseResultArgs = e as AntlrParseResultEventArgs;
            List<ITagSpan<IOutliningRegionTag>> outliningRegions = new List<ITagSpan<IOutliningRegionTag>>();
            if (antlrParseResultArgs != null)
            {
                IAstRuleReturnScope resultArgs = antlrParseResultArgs.Result as IAstRuleReturnScope;
                CommonTree result = resultArgs != null ? resultArgs.Tree as CommonTree : null;
                if (result != null && result.Children != null)
                {
                    foreach (CommonTree child in result.Children)
                    {
                        if (child == null || string.IsNullOrEmpty(child.Text))
                            continue;

                        switch (child.Type)
                        {
                        case GoLexer.KW_IMPORT:
                        case GoLexer.KW_TYPE:
                        case GoLexer.KW_CONST:
                        case GoLexer.KW_FUNC:
                            var startToken = antlrParseResultArgs.Tokens[child.TokenStartIndex];
                            var stopToken = antlrParseResultArgs.Tokens[child.TokenStopIndex];
                            Span span = new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1);
                            SnapshotSpan snapshotSpan = new SnapshotSpan(e.Snapshot, span);
                            IOutliningRegionTag tag = new OutliningRegionTag();
                            TagSpan<IOutliningRegionTag> tagSpan = new TagSpan<IOutliningRegionTag>(snapshotSpan, tag);
                            outliningRegions.Add(tagSpan);
                            break;

                        default:
                            continue;
                        }
                    }
                }
            }

            this._outliningRegions = outliningRegions;
            OnTagsChanged(new SnapshotSpanEventArgs(new SnapshotSpan(e.Snapshot, new Span(0, e.Snapshot.Length))));
        }
    }
}
