using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GitHubTracker
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("TodoGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(TodoTag))]
    internal sealed class TodoGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new TodoGlyphFactory();
        }

        private class TodoGlyphFactory : IGlyphFactory
        {
            const double m_glyphSize = 16.0;

            public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
            {
                // Ensure we can draw a glyph for this marker.
                if (tag == null || !(tag is TodoTag))
                {
                    return null;
                }

                return new Rectangle
                {
                    Fill = Brushes.SteelBlue,
                    Height = m_glyphSize,
                    Width = m_glyphSize
                };
            }
        }
    }

    internal class TodoTag : IGlyphTag
    {
        public TodoTag(string organization, string repo, int issue)
        {
            Organization = organization;
            Repo = repo;
            Issue = issue;
        }

        public string Organization { get; }

        public string Repo { get; }

        public int Issue { get; }
    }

    internal class TodoTagger : ITagger<TodoTag>
    {
        private static readonly Regex s_regex = new Regex(@"GitHub\W+(\w+)/(\w+)\W+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private IClassifier _classifier;

        public TodoTagger(IClassifier classifier)
        {
            _classifier = classifier;
        }

        public IEnumerable<ITagSpan<TodoTag>> GetTags(NormalizedSnapshotSpanCollection spans)
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
                            yield return new TagSpan<TodoTag>(new SnapshotSpan(classification.Span.Start + match.Index, match.Value.Length), new TodoTag(match.Groups[1].Value, match.Groups[2].Value, Convert.ToInt32(match.Groups[3].Value)));
                        }
                    }
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(TodoTag))]
    internal class TodoTaggerProvider : ITaggerProvider
    {
        [Import]
        public IClassifierAggregatorService AggregatorService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return new TodoTagger(AggregatorService.GetClassifier(buffer)) as ITagger<T>;
        }
    }

}
