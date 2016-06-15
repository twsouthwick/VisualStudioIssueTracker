using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
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

}
