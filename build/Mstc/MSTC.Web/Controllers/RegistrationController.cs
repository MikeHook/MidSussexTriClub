using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.configuration;
using Mstc.Core.Domain;
using MSTC.Web.Model;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class RegistrationController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegistrationDetails model)
        {
            if (ModelState.IsValid)
            {
                
            }
            return CurrentUmbracoPage();
        }

     
    }
}