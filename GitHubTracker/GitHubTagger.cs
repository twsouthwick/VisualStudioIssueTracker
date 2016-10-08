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
        private readonly ITagAggregator<IClassificationTag> _tags;
        private readonly IGitHubClient _client;

        public GitHubTagger(ITextView textView, ITagAggregator<IClassificationTag> tags, IGitHubClient client)
        {
            _textView = textView;
            _tags = tags;
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
                foreach (var tag in _tags.GetTags(span))
                {
                    if (tag.Tag.ClassificationType.IsOfType(Comment))
                    {
                        foreach (var snapShot in tag.Span.GetSpans(_textView.TextSnapshot))
                        {
                            var text = snapShot.GetText();
                            var matches = s_regex.Matches(text);

                            foreach (Match match in matches)
                            {
                                yield return new TagSpan<GitHubTag>(
                                    new SnapshotSpan(snapShot.Start + match.Index, match.Value.Length),
                                    new GitHubTag(match.Groups[1].Value, match.Groups[2].Value, Convert.ToInt32(match.Groups[3].Value), _client));
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
