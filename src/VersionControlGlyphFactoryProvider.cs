using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IssueTracker
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("VersionControlGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(Providers.GitHub.GitHubTag))]
    internal sealed class VersionControlGlyphFactoryProvider : IGlyphFactoryProvider
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
                var versionControlTag = tag as IVersionControlTag;

                // Ensure we can draw a glyph for this marker.
                if (versionControlTag == null)
                {
                    return null;
                }

                var border = new Border
                {
                    Height = m_glyphSize,
                    Width = m_glyphSize,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Black
                };

                var rectangle = new Rectangle
                {
                    Height = m_glyphSize,
                    Width = m_glyphSize,
                };

                border.Child = rectangle;

                versionControlTag.Update(t =>
                {
                    rectangle.Dispatcher.Invoke(() =>
                    {
                        switch (t)
                        {
                            case IssueStatus.Closed:
                                rectangle.Fill = Brushes.Green;
                                break;
                            case IssueStatus.Open:
                                rectangle.Fill = Brushes.Red;
                                break;
                            case IssueStatus.Unavailable:
                                rectangle.Fill = Brushes.CadetBlue;
                                break;
                            case IssueStatus.RateLimited:
                                rectangle.Fill = Brushes.AliceBlue;
                                break;
                        }
                    });
                });

                return border;
            }
        }
    }
}
