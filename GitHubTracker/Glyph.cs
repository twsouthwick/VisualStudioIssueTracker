using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GitHubTracker
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("GitHubGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(GitHubTag))]
    internal sealed class GitHubGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new GitHubGlyphFactory();
        }

        private class GitHubGlyphFactory : IGlyphFactory
        {
            const double m_glyphSize = 16.0;

            public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
            {
                var gitHubTag = tag as GitHubTag;

                // Ensure we can draw a glyph for this marker.
                if (gitHubTag == null)
                {
                    return null;
                }

                var rectangle = new Rectangle
                {
                    Height = m_glyphSize,
                    Width = m_glyphSize
                };

                gitHubTag.Update(t =>
                {
                    if (string.Equals("closed", t, StringComparison.Ordinal))
                    {
                        rectangle.Dispatcher.Invoke(() => rectangle.Fill = Brushes.Green);
                    }
                    else
                    {
                        rectangle.Dispatcher.Invoke(() => rectangle.Fill = Brushes.Red);
                    }
                });

                return rectangle;
            }
        }
    }

    internal class GitHubTag : IGlyphTag
    {
        // TODO: Inject in to control lifetime
        private static readonly HttpClient s_client = new HttpClient();

        private Task<string> _task;

        public GitHubTag(string organization, string repo, int issue)
        {
            Organization = organization;
            Repo = repo;
            Issue = issue;

            _task = Task.Run(() =>
            {
                return GetStatusAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debugger.Break();

                        return string.Empty;
                    }

                    return t.Result;
                });
            });
        }

        public string Organization { get; }

        public string Repo { get; }

        public int Issue { get; }

        public void Update(Action<string> action)
        {
            Debug.Assert(action != null);

            _task = _task.ContinueWith(t =>
            {
                action(t.Result);
                return t.Result;
            });
        }

        private async Task<string> GetStatusAsync()
        {
            using (var message = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Organization}/{Repo}/issues/{Issue}"))
            {
                message.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));

                var result = await s_client.SendAsync(message);

                if (!result.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var serializer = JsonSerializer.CreateDefault();

                using (var content = await result.Content.ReadAsStreamAsync())
                using (var textReader = new StreamReader(content))
                using (var reader = new JsonTextReader(textReader))
                {
                    var issue = serializer.Deserialize<IssueResponse>(reader);

                    return issue.State;
                }
            }
        }

        private class IssueResponse
        {
            public string State { get; set; }
        }
    }

    internal class GitHubTagger : ITagger<GitHubTag>
    {
        private static readonly Regex s_regex = new Regex(@"GitHub\W+(\w+)/(\w+)\W+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private IClassifier _classifier;

        public GitHubTagger(IClassifier classifier)
        {
            _classifier = classifier;
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
                                new GitHubTag(match.Groups[1].Value, match.Groups[2].Value, Convert.ToInt32(match.Groups[3].Value)));
                        }
                    }
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(GitHubTag))]
    internal class GitHubTaggerProvider : ITaggerProvider
    {
        [Import]
        public IClassifierAggregatorService AggregatorService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return new GitHubTagger(AggregatorService.GetClassifier(buffer)) as ITagger<T>;
        }
    }
}
