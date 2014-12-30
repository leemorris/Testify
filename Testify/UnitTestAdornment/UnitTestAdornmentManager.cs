using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestAdornmentManager
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private ITextBuffer _buffer;


        private UnitTestAdornmentManager(IWpfTextView view)
        {
            _view = view;
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;

            _layer = view.GetAdornmentLayer("PostAdornmentLayer");

           // this.provider = UnitTestAdornmentProvider.Create(view);
           // this.provider.PostsChanged += OnPostsChanged;
        }

        public static UnitTestAdornmentManager Create(IWpfTextView textView)
        {
           
            return textView.Properties.GetOrCreateSingletonProperty<UnitTestAdornmentManager>(delegate { return new UnitTestAdornmentManager(textView); });
 
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            Detach();
            //Get all of the posts that intersect any of the new or reformatted lines of text.
            //List<UnitTestSelector> newPosts = new List<UnitTestSelector>();

            //The event args contain a list of modified lines and a NormalizedSpanCollection of the spans of the modified lines. 
            //Use the latter to find the posts that intersect the new or reformatted lines of text.
            //foreach (Span span in e.NewOrReformattedSpans)
            //{
            //    newPosts.AddRange(this.provider.GetPosts(new SnapshotSpan(this.view.TextSnapshot, span)));
            //}

            ////It is possible to get duplicates in this list if a post spanned 3 lines, and the first and last lines were modified but the middle line was not.
            ////Sort the list and skip duplicates.
            ////newPosts.Sort(delegate(UnitTestSelector a, PostAdornment b) { return a.GetHashCode().CompareTo(b.GetHashCode()); });

            //UnitTestSelector lastPost = null;
            //foreach (UnitTestSelector post in newPosts)
            //{
            //    if (post != lastPost)
            //    {
            //        lastPost = post;
            //        this.DrawPost(post);
            //    }
            //}
            
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Detach();
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
        }

        public void DisplayUnitTestSelector(UnitTestAdornment coveredLineInfo)
        {
            SnapshotSpan span = coveredLineInfo.Span.GetSpan(this._view.TextSnapshot);
            //Geometry g = this.view.TextViewLines.GetMarkerGeometry(span);

            //if (g != null)
            //{
                //Find the rightmost coordinate of all the lines that intersect the adornment.
                double maxRight = 0.0;
                foreach (ITextViewLine line in this._view.TextViewLines.GetTextViewLinesIntersectingSpan(span))
                    maxRight = Math.Max(maxRight, line.Right);

                var vertPos = this._view.ViewportTop + coveredLineInfo.YPosition + .5 * this._view.LineHeight;
                 //Create the visualization.
                var selector = new UnitTestSelector(vertPos, coveredLineInfo, this._layer);

                //Add it to the layer.
                _layer.AddAdornment(span, coveredLineInfo, selector);
            //}
        }

        public void Detach()
        {
            if (_buffer != null)
            {
                //remove the Changed listener
                _buffer.Changed -= OnBufferChanged;
                _buffer = null;
            }
        }


        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            //Make a list of all posts that have a span of at least one character after applying the change. There is no need to raise a changed event for the deleted adornments. The adornments are deleted only if a text change would cause the view to reformat the line and discard the adornments.
            //IList<PostAdornment> keptPosts = new List<PostAdornment>(this.posts.Count);

            //foreach (PostAdornment post in this.posts)
            //{
            //    Span span = post.Span.GetSpan(e.After);
            //    //if a post does not span at least one character, its text was deleted.
            //    if (span.Length != 0)
            //    {
            //        keptPosts.Add(post);
            //    }
            //}

            //this.posts = keptPosts;
        }

      
    }
}
