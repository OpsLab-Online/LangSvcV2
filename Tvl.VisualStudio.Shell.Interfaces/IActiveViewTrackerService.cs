﻿namespace Tvl.VisualStudio.Shell
{
    using System;
    using Microsoft.VisualStudio.Text.Editor;

    public interface IActiveViewTrackerService
    {
        event EventHandler<ViewChangedEventArgs> ViewChanged;

        event EventHandler<ViewChangedEventArgs> ViewWithMouseChanged;

        ITextView ActiveView
        {
            get;
        }

        ITextView ViewWithMouse
        {
            get;
        }
    }
}
