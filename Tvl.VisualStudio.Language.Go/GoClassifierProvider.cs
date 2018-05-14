﻿namespace Tvl.VisualStudio.Language.Go
{
    using System.ComponentModel.Composition;
    using JetBrains.Annotations;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Utilities;
    using Tvl.VisualStudio.Text.Classification;

    [Export(typeof(IClassifierProvider))]
    [ContentType(GoConstants.GoContentType)]
    public sealed class GoClassifierProvider : LanguageClassifierProvider<GoLanguagePackage>
    {
        protected override IClassifier GetClassifierImpl([NotNull] ITextBuffer textBuffer)
        {
            return new GoClassifier(textBuffer, StandardClassificationService, ClassificationTypeRegistryService);
        }
    }
}
