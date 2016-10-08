using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace IssueTracker
{
    internal interface IVersionControlClassifier
    {
        IEnumerable<ITagSpan<IVersionControlTag>> GetTags(string text, SnapshotSpan snapShot);
    }
}