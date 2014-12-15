using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Leem.Testify.UnitTestAdornment
{
    public class UnitTestAdornmentManager
    {
        private readonly IWpfTextView view;
        private readonly IAdornmentLayer layer;
        private ITextBuffer buffer;


        private UnitTestAdornmentManager(IWpfTextView view)
        {
            this.view = view;
            this.view.LayoutChanged += OnLayoutChanged;
            this.view.Closed += OnClosed;

            this.layer = view.GetAdornmentLayer("PostAdornmentLayer");

           // this.provider = UnitTestAdornmentProvider.Create(view);
           // this.provider.PostsChanged += OnPostsChanged;
        }

        public static UnitTestAdornmentManager Create(IWpfTextView textView)
        {
           
            return textView.Properties.GetOrCreateSingletonProperty<UnitTestAdornmentManager>(delegate { return new UnitTestAdornmentManager(textView); });
 
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            this.Detach();
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
            this.Detach();
            this.view.LayoutChanged -= OnLayoutChanged;
            this.view.Closed -= OnClosed;
        }

        public void DisplayUnitTestSelector(UnitTestAdornment coveredLineInfo)
        {
            SnapshotSpan span = coveredLineInfo.Span.GetSpan(this.view.TextSnapshot);
            //Geometry g = this.view.TextViewLines.GetMarkerGeometry(span);

            //if (g != null)
            //{
                //Find the rightmost coordinate of all the lines that intersect the adornment.
                double maxRight = 0.0;
                foreach (ITextViewLine line in this.view.TextViewLines.GetTextViewLinesIntersectingSpan(span))
                    maxRight = Math.Max(maxRight, line.Right);

                var vertPos = this.view.ViewportTop + coveredLineInfo.YPosition + .5 * this.view.LineHeight;
                 //Create the visualization.
                UnitTestSelector selector = new UnitTestSelector(vertPos, coveredLineInfo, this.layer);

                //Add it to the layer.
                this.layer.AddAdornment(span, coveredLineInfo, selector);
            //}
        }

        public void Detach()
        {
            if (this.buffer != null)
            {
                //remove the Changed listener
                this.buffer.Changed -= OnBufferChanged;
                this.buffer = null;
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
