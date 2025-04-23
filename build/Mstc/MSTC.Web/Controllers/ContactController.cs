using System.Text;
using System.Web.Mvc;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class ContactController : SurfaceController
    {
        EmailProvider _emailProvider;

        public ContactController()
        {
            _emailProvider = new EmailProvider();
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(ContactModel model)
        {
            if (ModelState.IsValid)
            {
                string toAddress;
                if (_emailProvider.EmailLookup.TryGetValue(model.Topic, out toAddress) == false)
                    toAddress = "support@midsussextriclub.com";

                string content = $"<p>{model.Message}</p><p>Message from: {model.Name}</p><p>Email: {model.Email}</p>";

                _emailProvider.SendEmail(toAddress, model.Email, "Mid Sussex Tri Club Website Enquiry", content);

                TempData["Message"] = "Thank you for contacting us, we will respond soon.";
            }
            return CurrentUmbracoPage();
        }

       
      
       
    }
}
