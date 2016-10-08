using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Concurrent;

namespace GitHubTracker
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(GitHubTag))]
    internal class GitHubViewTaggerProvider : IViewTaggerProvider
    {
        private readonly IGitHubClient _client;
        private readonly IViewTagAggregatorFactoryService _tagAggregator;

        private readonly ConcurrentDictionary<ITextBuffer, GitHubTagger> _taggers = new ConcurrentDictionary<ITextBuffer, GitHubTagger>();

        [ImportingConstructor]
        public GitHubViewTaggerProvider(IGitHubClient client, IViewTagAggregatorFactoryService tagAggregator)
        {
            _tagAggregator = tagAggregator;
            _client = client;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null | textView == null)
            {
                return null;
            }

            var tags = _tagAggregator.CreateTagAggregator<IClassificationTag>(textView);

            return new GitHubTagger(textView, tags, _client) as ITagger<T>;
        }
    }
}
