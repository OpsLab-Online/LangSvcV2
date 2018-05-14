﻿namespace Tvl.VisualStudio.Language.Chapel
{
    using Antlr.Runtime;
    using JetBrains.Annotations;

    partial class ChapelCodeClassifierLexer
    {
        private readonly ChapelClassifierLexer _lexer;

        public ChapelCodeClassifierLexer(ICharStream input, [NotNull] ChapelClassifierLexer lexer)
            : this(input)
        {
            Requires.NotNull(lexer, nameof(lexer));

            _lexer = lexer;
        }

        private bool InComment
        {
            get
            {
                return _lexer.InComment;
            }

            set
            {
                _lexer.InComment = value;
            }
        }

        public override IToken NextToken()
        {
            IToken token = base.NextToken();

            switch (token.Type)
            {
            case CONTINUE_COMMENT:
                InComment = true;
                token.Type = ML_COMMENT;
                break;

            case END_COMMENT:
                InComment = false;
                token.Type = ML_COMMENT;
                break;

            default:
                break;
            }

            return token;
        }

        protected override void ParseNextToken()
        {
            if (InComment)
                mCONTINUE_COMMENT();
            else
                base.ParseNextToken();
        }

        public override void Recover(IIntStream input, RecognitionException re)
        {
            base.Recover(input, re);
        }

        public override void DisplayRecognitionError(string[] tokenNames, RecognitionException e)
        {
            base.DisplayRecognitionError(tokenNames, e);
        }
    }
}
