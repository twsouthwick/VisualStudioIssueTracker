using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitHubTracker
{
    internal class GitHubTagger : ITagger<GitHubTag>
    {
        private const string Comment = "comment";

        private static readonly Regex s_regex = new Regex(@"GitHub\W+(\w+)/(\w+)\W+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ITextView _textView;
        private readonly IClassifier _classifier;
        private readonly IGitHubClient _client;

        public GitHubTagger(ITextView textView, IClassifier classifier, IGitHubClient client)
        {
            _textView = textView;
            _classifier = classifier;
            _client = client;
        }

        public IEnumerable<ITagSpan<GitHubTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            foreach (SnapshotSpan span in spans)
            {
                var text = span.GetText();
                //look at each classification span \
                foreach (ClassificationSpan classification in _classifier.GetClassificationSpans(span))
                {
                    if (classification.ClassificationType.IsOfType(Comment))
                    {
                        var matches = s_regex.Matches(classification.Span.GetText());

                        foreach (Match match in matches)
                        {
                            yield return new TagSpan<GitHubTag>(
                                new SnapshotSpan(classification.Span.Start + match.Index, match.Value.Length),
                                new GitHubTag(match.Groups[1].Value, match.Groups[2].Value, Convert.ToInt32(match.Groups[3].Value), _client));
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
