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
        [Import]
        public GitHubHttpClient Client { get; set; }

        [Import]
        public IClassifierAggregatorService AggregatorService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer)
            where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return new GitHubTagger(AggregatorService.GetClassifier(buffer), Client) as ITagger<T>;
        }
    }
}
