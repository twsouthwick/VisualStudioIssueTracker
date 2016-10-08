using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace IssueTracker.Providers.GitHub
{
    [Export(typeof(IVersionControlClassifier))]
    internal class GitHubClassifier : IVersionControlClassifier
    {
        private static readonly Regex s_regex = new Regex(@"GitHub\W+(\w+)/(\w+)\W+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IGitHubClient _client;

        [ImportingConstructor]
        public GitHubClassifier(IGitHubClient client)
        {
            _client = client;
        }

        public IEnumerable<ITagSpan<IVersionControlTag>> GetTags(string text, SnapshotSpan snapShot)
        {
            var matches = s_regex.Matches(text);

            if (matches.Count == 0)
            {
                yield break;
            }

            foreach (Match match in matches)
            {
                var tag = new GitHubTag(match.Groups[1].Value, match.Groups[2].Value, Convert.ToInt32(match.Groups[3].Value), _client);
                var span = new SnapshotSpan(snapShot.Start + match.Index, match.Value.Length);

                tag.Initialize();

                yield return new TagSpan<GitHubTag>(span, tag);
            }
        }
    }
}