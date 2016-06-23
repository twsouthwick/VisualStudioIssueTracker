using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace GitHubTracker
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(GitHubTag))]
    internal class GitHubTaggerProvider : ITaggerProvider
    {
        private readonly IGitHubClient _client;
        private readonly IClassifierAggregatorService _aggregatorService;

        [ImportingConstructor]
        public GitHubTaggerProvider(IGitHubClient client, IClassifierAggregatorService aggregatorService)
        {
            _aggregatorService = aggregatorService;
            _client = client;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer)
            where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return new GitHubTagger(_aggregatorService.GetClassifier(buffer), _client) as ITagger<T>;
        }
    }
}
