using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model.Partials
{
    public class MemberImageModel
    {
        public string ProfileImageId { get; set; }

        public HttpPostedFileBase ProfileImageFile { get; set; }

        public bool RemoveImage { get; set; }
    }
}