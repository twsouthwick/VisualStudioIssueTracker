using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace IssueTracker
{
    internal class VersionControlTagger : ITagger<IVersionControlTag>
    {
        private const string Comment = "comment";

        private readonly ITextView _textView;
        private readonly ITagAggregator<IClassificationTag> _tags;
        private readonly IVersionControlClassifier[] _classifiers;

        public VersionControlTagger(ITextView textView, ITagAggregator<IClassificationTag> tags, IVersionControlClassifier[] classifiers)
        {
            _textView = textView;
            _tags = tags;
            _classifiers = classifiers;
        }

        public IEnumerable<ITagSpan<IVersionControlTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            foreach (SnapshotSpan span in spans)
            {
                foreach (var tag in _tags.GetTags(span))
                {
                    if (tag.Tag.ClassificationType.IsOfType(Comment))
                    {
                        foreach (var snapShot in tag.Span.GetSpans(_textView.TextSnapshot))
                        {
                            var text = snapShot.GetText();

                            foreach (var classifier in _classifiers)
                            {
                                foreach(var versionControlTag in classifier.GetTags(text, snapShot))
                                {
                                    yield return versionControlTag;
                                }
                            }
                        }
                    }
                }
            }
        }

#pragma warning disable 0067
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 0067
    }
}
