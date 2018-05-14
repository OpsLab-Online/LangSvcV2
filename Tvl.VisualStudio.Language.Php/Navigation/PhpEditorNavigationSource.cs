﻿namespace Tvl.VisualStudio.Language.Php.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using JetBrains.Annotations;
    using Microsoft.VisualStudio.Text;
    using Tvl.VisualStudio.Language.Parsing4;
    using Tvl.VisualStudio.Language.Php.Parser;
    using Tvl.VisualStudio.Text.Navigation;
    using ImageSource = System.Windows.Media.ImageSource;
    using ParseResultEventArgs = Tvl.VisualStudio.Language.Parsing.ParseResultEventArgs;
    using StandardGlyphGroup = Microsoft.VisualStudio.Language.Intellisense.StandardGlyphGroup;
    using StandardGlyphItem = Microsoft.VisualStudio.Language.Intellisense.StandardGlyphItem;

    internal sealed class PhpEditorNavigationSource : IEditorNavigationSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly PhpEditorNavigationBackgroundParser _backgroundParser;
        private readonly PhpEditorNavigationSourceProvider _provider;

        private List<IEditorNavigationTarget> _navigationTargets;

        public event EventHandler NavigationTargetsChanged;

        public PhpEditorNavigationSource([NotNull] ITextBuffer textBuffer, PhpEditorNavigationSourceProvider provider)
        {
            Requires.NotNull(textBuffer, nameof(textBuffer));

            this._textBuffer = textBuffer;
            this._backgroundParser = PhpEditorNavigationBackgroundParser.CreateParser(textBuffer, provider.BackgroundIntelliSenseTaskScheduler, provider.OutputWindowService, provider.TextDocumentFactoryService);
            this._provider = provider;

            this.BackgroundParser.ParseComplete += HandleBackgroundParseComplete;
            this.BackgroundParser.RequestParse(false);
        }

        internal PhpEditorNavigationSourceProvider Provider
        {
            get
            {
                return _provider;
            }
        }

        internal PhpEditorNavigationBackgroundParser BackgroundParser
        {
            get
            {
                return _backgroundParser;
            }
        }

        public IEnumerable<IEditorNavigationType> GetNavigationTypes()
        {
            yield return _provider.EditorNavigationTypeRegistryService.GetEditorNavigationType(PredefinedEditorNavigationTypes.Types);
            yield return _provider.EditorNavigationTypeRegistryService.GetEditorNavigationType(PredefinedEditorNavigationTypes.Members);
        }

        public IEnumerable<IEditorNavigationTarget> GetNavigationTargets()
        {
            return _navigationTargets ?? Enumerable.Empty<IEditorNavigationTarget>();
        }

        private void OnNavigationTargetsChanged(EventArgs e)
        {
            var t = NavigationTargetsChanged;
            if (t != null)
                t(this, e);
        }

        private void HandleBackgroundParseComplete(object sender, ParseResultEventArgs e)
        {
            PhpEditorNavigationParseResultEventArgs antlrParseResultArgs = e as PhpEditorNavigationParseResultEventArgs;
            if (antlrParseResultArgs == null)
                return;

            UpdateNavigationTargets(antlrParseResultArgs);
        }

        private void UpdateNavigationTargets([NotNull] PhpEditorNavigationParseResultEventArgs antlrParseResultArgs)
        {
            Debug.Assert(antlrParseResultArgs != null);

            List<IEditorNavigationTarget> navigationTargets = new List<IEditorNavigationTarget>();

            // always add the "global scope" element
            {
                string name = "Global Scope";
                IEditorNavigationType editorNavigationType = Provider.EditorNavigationTypeRegistryService.GetEditorNavigationType(PredefinedEditorNavigationTypes.Types);
                SnapshotSpan span = new SnapshotSpan(antlrParseResultArgs.Snapshot, 0, antlrParseResultArgs.Snapshot.Length);
                SnapshotSpan seek = new SnapshotSpan(span.Start, 0);
                ImageSource glyph = Provider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupNamespace, StandardGlyphItem.GlyphItemPublic);
                NavigationTargetStyle style = NavigationTargetStyle.None;
                navigationTargets.Add(new EditorNavigationTarget(name, editorNavigationType, span, seek, glyph, style));
            }

            if (antlrParseResultArgs != null)
            {
                ITextSnapshot snapshot = antlrParseResultArgs.Snapshot;

                Listener listener = new Listener(Provider, snapshot, antlrParseResultArgs, navigationTargets);

                foreach (var tree in antlrParseResultArgs.NavigationTrees)
                {
                    ParseTreeWalkers.SingleTree.Walk(listener, tree);
                }
#if false
                IAstRuleReturnScope resultArgs = antlrParseResultArgs.Result as IAstRuleReturnScope;
                var result = resultArgs != null ? resultArgs.Tree as CommonTree : null;
                if (result != null)
                {
                    foreach (CommonTree child in result.Children)
                    {
                        if (child == null || string.IsNullOrEmpty(child.Text))
                            continue;

                        if (child.Text == "rule" && child.ChildCount > 0)
                        {
                            var ruleName = child.GetChild(0).Text;
                            if (string.IsNullOrEmpty(ruleName))
                                continue;

                            if (ruleName == "Tokens")
                                continue;

                            var navigationType = char.IsUpper(ruleName[0]) ? _lexerRuleNavigationType : _parserRuleNavigationType;
                            IToken startToken = antlrParseResultArgs.Tokens[child.TokenStartIndex];
                            IToken stopToken = antlrParseResultArgs.Tokens[child.TokenStopIndex];
                            Span span = new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1);
                            SnapshotSpan ruleSpan = new SnapshotSpan(antlrParseResultArgs.Snapshot, span);
                            SnapshotSpan ruleSeek = new SnapshotSpan(antlrParseResultArgs.Snapshot, new Span(((CommonTree)child.GetChild(0)).Token.StartIndex, 0));
                            var glyph = char.IsUpper(ruleName[0]) ? _lexerRuleGlyph : _parserRuleGlyph;
                            navigationTargets.Add(new EditorNavigationTarget(ruleName, navigationType, ruleSpan, ruleSeek, glyph));
                        }
                        else if (child.Text.StartsWith("tokens"))
                        {
                            foreach (CommonTree tokenChild in child.Children)
                            {
                                if (tokenChild.Text == "=" && tokenChild.ChildCount == 2)
                                {
                                    var ruleName = tokenChild.GetChild(0).Text;
                                    if (string.IsNullOrEmpty(ruleName))
                                        continue;

                                    var navigationType = char.IsUpper(ruleName[0]) ? _lexerRuleNavigationType : _parserRuleNavigationType;
                                    IToken startToken = antlrParseResultArgs.Tokens[tokenChild.TokenStartIndex];
                                    IToken stopToken = antlrParseResultArgs.Tokens[tokenChild.TokenStopIndex];
                                    Span span = new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1);
                                    SnapshotSpan ruleSpan = new SnapshotSpan(antlrParseResultArgs.Snapshot, span);
                                    SnapshotSpan ruleSeek = new SnapshotSpan(antlrParseResultArgs.Snapshot, new Span(((CommonTree)tokenChild.GetChild(0)).Token.StartIndex, 0));
                                    var glyph = char.IsUpper(ruleName[0]) ? _lexerRuleGlyph : _parserRuleGlyph;
                                    navigationTargets.Add(new EditorNavigationTarget(ruleName, navigationType, ruleSpan, ruleSeek, glyph));
                                }
                                else if (tokenChild.ChildCount == 0)
                                {
                                    var ruleName = tokenChild.Text;
                                    if (string.IsNullOrEmpty(ruleName))
                                        continue;

                                    var navigationType = char.IsUpper(ruleName[0]) ? _lexerRuleNavigationType : _parserRuleNavigationType;
                                    IToken startToken = antlrParseResultArgs.Tokens[tokenChild.TokenStartIndex];
                                    IToken stopToken = antlrParseResultArgs.Tokens[tokenChild.TokenStopIndex];
                                    Span span = new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1);
                                    SnapshotSpan ruleSpan = new SnapshotSpan(antlrParseResultArgs.Snapshot, span);
                                    SnapshotSpan ruleSeek = new SnapshotSpan(antlrParseResultArgs.Snapshot, new Span(tokenChild.Token.StartIndex, 0));
                                    var glyph = char.IsUpper(ruleName[0]) ? _lexerRuleGlyph : _parserRuleGlyph;
                                    navigationTargets.Add(new EditorNavigationTarget(ruleName, navigationType, ruleSpan, ruleSeek, glyph));
                                }
                            }
                        }

                    }
                }
#endif
            }

            this._navigationTargets = navigationTargets;
            OnNavigationTargetsChanged(EventArgs.Empty);
        }

        private class Listener : PhpParserBaseListener
        {
            private readonly PhpEditorNavigationSourceProvider _provider;
            private readonly ITextSnapshot _snapshot;
            private readonly AntlrParseResultEventArgs _antlrParseResultArgs;
            private readonly ICollection<IEditorNavigationTarget> _navigationTargets;

            public Listener([NotNull] PhpEditorNavigationSourceProvider provider, [NotNull] ITextSnapshot snapshot, [NotNull] AntlrParseResultEventArgs antlrParseResultArgs, [NotNull] ICollection<IEditorNavigationTarget> navigationTargets)
            {
                Requires.NotNull(provider, nameof(provider));
                Requires.NotNull(snapshot, nameof(snapshot));
                Requires.NotNull(antlrParseResultArgs, nameof(antlrParseResultArgs));
                Requires.NotNull(navigationTargets, nameof(navigationTargets));

                _provider = provider;
                _snapshot = snapshot;
                _antlrParseResultArgs = antlrParseResultArgs;
                _navigationTargets = navigationTargets;
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_classOrInterfaceDefinition, 5, Dependents.Parents)]
            public override void EnterClassOrInterfaceDefinition(PhpParser.ClassOrInterfaceDefinitionContext context)
            {
                string name = GetQualifiedName(context);

                IEditorNavigationType navigationType = _provider.EditorNavigationTypeRegistryService.GetEditorNavigationType(PredefinedEditorNavigationTypes.Types);
                var startToken = _antlrParseResultArgs.Tokens[context.SourceInterval.a];
                var stopToken = _antlrParseResultArgs.Tokens[context.SourceInterval.b];
                SnapshotSpan span = new SnapshotSpan(_snapshot, new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1));
                SnapshotSpan seek = span;
                if (context.PHP_IDENTIFIER() != null)
                    seek = new SnapshotSpan(_snapshot, new Span(context.PHP_IDENTIFIER().Symbol.StartIndex, 0));

                StandardGlyphGroup glyphGroup;
                if (context.KW_INTERFACE() != null)
                {
                    glyphGroup = StandardGlyphGroup.GlyphGroupInterface;
                }
                else
                {
                    glyphGroup = StandardGlyphGroup.GlyphGroupClass;
                }

                //StandardGlyphItem glyphItem = GetGlyphItemFromChildModifier(child);
                StandardGlyphItem glyphItem = StandardGlyphItem.GlyphItemPublic;
                ImageSource glyph = _provider.GlyphService.GetGlyph(glyphGroup, glyphItem);
                NavigationTargetStyle style = NavigationTargetStyle.None;
                _navigationTargets.Add(new EditorNavigationTarget(name, navigationType, span, seek, glyph, style));
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionDefinition, 5, Dependents.Parents)]
            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionParameterList, 0, Dependents.Self)]
            public override void EnterFunctionDefinition(PhpParser.FunctionDefinitionContext context)
            {
                string name = GetName(context);
                IEnumerable<string> args = ProcessArguments(context.functionParameterList());
                string sig = string.Format("{0}({1})", name, string.Join(", ", args));
                IEditorNavigationType navigationType = _provider.EditorNavigationTypeRegistryService.GetEditorNavigationType(PredefinedEditorNavigationTypes.Members);
                var startToken = _antlrParseResultArgs.Tokens[context.SourceInterval.a];
                var stopToken = _antlrParseResultArgs.Tokens[context.SourceInterval.b];
                SnapshotSpan span = new SnapshotSpan(_snapshot, new Span(startToken.StartIndex, stopToken.StopIndex - startToken.StartIndex + 1));
                SnapshotSpan seek = span;
                if (context.PHP_IDENTIFIER() != null)
                    seek = new SnapshotSpan(_snapshot, new Span(context.PHP_IDENTIFIER().Symbol.StartIndex, 0));

                StandardGlyphGroup glyphGroup = StandardGlyphGroup.GlyphGroupMethod;
                //StandardGlyphItem glyphItem = GetGlyphItemFromChildModifier(tree);
                StandardGlyphItem glyphItem = StandardGlyphItem.GlyphItemPublic;
                ImageSource glyph = _provider.GlyphService.GetGlyph(glyphGroup, glyphItem);
                NavigationTargetStyle style = NavigationTargetStyle.None;
                _navigationTargets.Add(new EditorNavigationTarget(sig, navigationType, span, seek, glyph, style));
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_classOrInterfaceDefinition, 5, Dependents.Ancestors)]
            private static string GetQualifiedName(PhpParser.ClassOrInterfaceDefinitionContext context)
            {
                string name = GetName(context);
                for (RuleContext parent = context.Parent; parent != null; parent = parent.Parent)
                {
                    var defContext = parent as PhpParser.ClassOrInterfaceDefinitionContext;
                    if (defContext != null)
                    {
                        name = GetName(defContext) + "." + name;
                    }
                }

                return name;
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_classOrInterfaceDefinition, 0, Dependents.Self)]
            private static string GetName([NotNull] PhpParser.ClassOrInterfaceDefinitionContext context)
            {
                Debug.Assert(context != null);

                ITerminalNode nameNode = context.PHP_IDENTIFIER();
                if (nameNode == null)
                    return "?";

                string name = nameNode.Symbol.Text;
                if (string.IsNullOrEmpty(name))
                    return "?";

                return name;
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionDefinition, 2, Dependents.Self)]
            private static string GetName([NotNull] PhpParser.FunctionDefinitionContext context)
            {
                Debug.Assert(context != null);

                ITerminalNode nameNode = context.PHP_IDENTIFIER();
                if (nameNode == null)
                    return "?";

                string name = nameNode.Symbol.Text;
                if (string.IsNullOrEmpty(name))
                    return "?";

                return name;
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionParameter, 0, Dependents.Self)]
            private static string GetName([NotNull] PhpParser.FunctionParameterContext context)
            {
                Debug.Assert(context != null);

                ITerminalNode nameNode = context.PHP_IDENTIFIER();
                if (nameNode == null)
                    return "?";

                string name = nameNode.Symbol.Text;
                if (string.IsNullOrEmpty(name))
                    return "?";

                return name;
            }

            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionParameterList, 2, Dependents.Parents)]
            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionParameters, 0, Dependents.Self)]
            [RuleDependency(typeof(PhpParser), PhpParser.RULE_functionParameter, 0, Dependents.Self)]
            private IEnumerable<string> ProcessArguments(PhpParser.FunctionParameterListContext context)
            {
                if (context == null)
                    yield break;

                PhpParser.FunctionParametersContext functionParametersContext = context.functionParameters();
                if (functionParametersContext == null)
                    yield break;

                foreach (PhpParser.FunctionParameterContext argTree in functionParametersContext.functionParameter())
                {
                    bool byRef = argTree.AND() != null;
                    if (byRef)
                        yield return "&" + GetName(argTree);
                    else
                        yield return GetName(argTree);
                }
            }
        }

        public static class ParseTreeWalkers
        {
            public static ParseTreeWalker SingleTree
            {
                get
                {
                    return SingleTreeParseTreeWalker.SingleTreeParseTreeWalkerInstance;
                }
            }

            private sealed class SingleTreeParseTreeWalker : ParseTreeWalker
            {
                public static readonly SingleTreeParseTreeWalker SingleTreeParseTreeWalkerInstance = new SingleTreeParseTreeWalker();

                public override void Walk(IParseTreeListener listener, IParseTree t)
                {
                    IRuleNode ruleNode = t as IRuleNode;
                    if (ruleNode != null)
                    {
                        EnterRule(listener, ruleNode);
                        ExitRule(listener, ruleNode);
                        return;
                    }

                    base.Walk(listener, t);
                }
            }
        }
    }
}
