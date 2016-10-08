using Microsoft.VisualStudio.Text.Editor;
using System;

namespace IssueTracker
{
    internal interface IVersionControlTag : IGlyphTag
    {
        void Update(Action<IssueStatus> status);
    }
}
