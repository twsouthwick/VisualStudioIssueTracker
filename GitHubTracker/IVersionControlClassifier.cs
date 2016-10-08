using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace GitHubTracker
{
    internal interface IVersionControlClassifier
    {
        IEnumerable<ITagSpan<IVersionControlTag>> GetTags(string text, SnapshotSpan snapShot);
    }
}