using System;
using System.Configuration;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using MSTC.Web.Model.Partials;
using Newtonsoft.Json;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class MemberEditController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;


        public MemberEditController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
        }

        [HttpGet]
        public ActionResult MemberImage()
        {
            var model = new MemberImageModel()
            {

            };

            return PartialView("Member/EditMemberImage", model);
        }

        [HttpGet]
        public ActionResult MemberDetails()
        {
            var model = new MemberDetailsModel()
            {
               
            };
    
            return PartialView("Member/EditMemberDetails", model);
        }

        [HttpGet]
        public ActionResult MemberOptions()
        {
            var model = new MemberOptionsModel()
            {

            };

            return PartialView("Member/EditMemberOptions", model);
        }
    }
}