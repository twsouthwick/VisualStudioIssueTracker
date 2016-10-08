using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace GitHubTracker
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(GitHubTag))]
    internal class VersionControlViewTaggerProvider : IViewTaggerProvider
    {
        private readonly IVersionControlClassifier[] _classifiers;
        private readonly IViewTagAggregatorFactoryService _tagAggregator;

        [ImportingConstructor]
        public VersionControlViewTaggerProvider(
            [ImportMany]
            IVersionControlClassifier[] classifiers,
            IViewTagAggregatorFactoryService tagAggregator)
        {
            _tagAggregator = tagAggregator;
            _classifiers = classifiers;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null | textView == null)
            {
                return null;
            }

            var tags = _tagAggregator.CreateTagAggregator<IClassificationTag>(textView);

            return new VersionControlTagger(textView, tags, _classifiers) as ITagger<T>;
        }
    }
}
