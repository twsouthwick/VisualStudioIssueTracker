using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitHubTracker
{
    internal class GitHubTagger : ITagger<GitHubTag>
    {
        private static readonly Regex s_regex = new Regex(@"GitHub\W+(\w+)/(\w+)\W+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private IClassifier _classifier;
        private IGitHubClient _client;

        public GitHubTagger(IClassifier classifier, IGitHubClient client)
        {
            _classifier = classifier;
            _client = client;
        }

        public IEnumerable<ITagSpan<GitHubTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in spans)
            {
                //look at each classification span \
                foreach (ClassificationSpan classification in _classifier.GetClassificationSpans(span))
                {
                    if (string.Equals("comment", classification.ClassificationType.Classification, StringComparison.CurrentCultureIgnoreCase))
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

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
