using Umbraco.Web.PublishedContentModels;

namespace MSTC.Web.Model.Partials
{
    public class SingleBlogPostModel
    {
        public Blogpost Post { get; set; }
        public bool ShowArchiveLink { get; set; }
        public bool ShowPermaLink { get; set; }
    }
}