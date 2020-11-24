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
using MSTC.Web.Model;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class MemberController : SurfaceController
    {
        public ActionResult RenderLogin()
        {
            return PartialView("Login", new LoginModel() { CurrentPage = CurrentPage });
        }

        public ActionResult RenderForgotPassword()
        {
            return PartialView("ForgotPassword", new ForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitLogin(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.Username, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.Username, false);
                    UrlHelper myHelper = new UrlHelper(HttpContext.Request.RequestContext);
                    if (myHelper.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return Redirect("/login/");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The username or password provided is incorrect.");
                }
            }
            return CurrentUmbracoPage();
        }

        public ActionResult RenderLogout()
        {
            return PartialView("Logout", null);
        }

        public ActionResult SubmitLogout()
        {
            TempData.Clear();
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var member = Services.MemberService.GetByEmail(model.Email);

                if (member == null)
                {
                    ModelState.AddModelError("", "Unable to find user with that email address.");
                    return CurrentUmbracoPage();
                }

                var newPassword = Membership.GeneratePassword(10, 1);
                newPassword = Regex.Replace(newPassword, @"[^a-zA-Z0-9]", m => "9");
                Services.MemberService.SavePassword(member, newPassword);         

                var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress("noreply@midsussextriclub.com");
                mailMessage.To.Add(model.Email);
                //objMail.From = new MailAddress(txtEmail.Text);
                mailMessage.Subject = "Mid Sussex Tri Club password reset";

                mailMessage.IsBodyHtml = true;

                var sb = new StringBuilder();
                sb.AppendFormat(string.Format("<p>Please find your new password below to access the site</p>"));
                sb.AppendFormat("<p><b>{0}</b></p>", newPassword);
                mailMessage.Body = sb.ToString();

                var gmailSmtpClient = new GmailSmtpClient();
                gmailSmtpClient.Send(mailMessage);

                return Redirect("/login/");

            }
            return CurrentUmbracoPage();
        }
    }
}