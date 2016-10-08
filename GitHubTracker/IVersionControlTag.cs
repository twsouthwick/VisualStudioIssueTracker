using Microsoft.VisualStudio.Text.Editor;
using System;

namespace GitHubTracker
{
    internal interface IVersionControlTag : IGlyphTag
    {
        void Update(Action<IssueStatus> status);
    }
}
