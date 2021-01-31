using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model
{
    public class BookEventResponse
    {
        public bool HasError => !string.IsNullOrEmpty(Error);
        public string Error { get; set; }
    }
}