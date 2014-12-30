namespace Leem.Testify.UnitTestAdornment
{
    //public class UnitTestAdornmentProvider
    //{
    //    private ITextBuffer buffer;
    //    //private IList<PostAdornment> posts = new List<PostAdornment>();

    //    private UnitTestAdornmentProvider(ITextBuffer buffer)
    //    {
    //        this.buffer = buffer;
    //        //listen to the Changed event so we can react to deletions.
    //        this.buffer.Changed += OnBufferChanged;
    //    }

    //    public static UnitTestAdornmentProvider Create(IWpfTextView view)
    //    {
    //        return view.Properties.GetOrCreateSingletonProperty<UnitTestAdornmentProvider>(delegate { return new UnitTestAdornmentProvider(view.TextBuffer); });
    //    }

    //    public void Detach()
    //    {
    //        if (this.buffer != null)
    //        {
    //            //remove the Changed listener
    //            this.buffer.Changed -= OnBufferChanged;
    //            this.buffer = null;
    //        }
    //    }

    //    public void Add(SnapshotSpan snapshotSpan, List<Poco.UnitTest> unitTests)
    //    {
    //        throw new NotImplementedException();
    //    }


    //       private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
    //    {
    //        //Make a list of all posts that have a span of at least one character after applying the change. There is no need to raise a changed event for the deleted adornments. The adornments are deleted only if a text change would cause the view to reformat the line and discard the adornments.
    //        //IList<PostAdornment> keptPosts = new List<PostAdornment>(this.posts.Count);

    //        //foreach (PostAdornment post in this.posts)
    //        //{
    //        //    Span span = post.Span.GetSpan(e.After);
    //        //    //if a post does not span at least one character, its text was deleted.
    //        //    if (span.Length != 0)
    //        //    {
    //        //        keptPosts.Add(post);
    //        //    }
    //        //}

    //        //this.posts = keptPosts;
    //    }


    //      
    //}
}