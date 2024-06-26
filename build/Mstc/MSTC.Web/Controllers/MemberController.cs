﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.configuration;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class MemberController : SurfaceController
    {
        EmailProvider _emailProvider;

        public MemberController()
        {
            _emailProvider = new EmailProvider();
        }

        public ActionResult RenderLogin()
        {
            return PartialView("Login", new LoginModel() { CurrentPage = CurrentPage });
        }

        public ActionResult RenderForgotPassword()
        {
            return PartialView("ForgotPassword", new ForgotPasswordModel());
        }

        public ActionResult RenderChangePassword()
        {
            return PartialView("ChangePassword", new ChangePasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitLogin(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.Username, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.Username, false);                    
                    return Redirect(model.RedirectUrl ?? "/login/");                    
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
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var username = Membership.GetUser().UserName;
                var member = Services.MemberService.GetByUsername(username);                

                if (member == null)
                {
                    ModelState.AddModelError("", "Unable to find logged in member.");
                    return CurrentUmbracoPage();
                }                
        
                if (!Membership.ValidateUser(username, model.OldPassword))
                {
                    ModelState.AddModelError("", "Your old password does not match.");
                    return CurrentUmbracoPage();
                }

                if (model.NewPassword.Length < 8)
                {
                    ModelState.AddModelError("", "Your new password must be at least 8 characters.");
                    return CurrentUmbracoPage();
                }

                Services.MemberService.SavePassword(member, model.NewPassword);

                TempData["Message"] = "Your password has been changed.";
            }
            return CurrentUmbracoPage();
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

                var sb = new StringBuilder();
                sb.AppendFormat(string.Format("<p>Please find your new password below to access the site</p>"));
                sb.AppendFormat("<p><b>{0}</b></p>", newPassword);
                _emailProvider.SendEmail(model.Email, "noreply@midsussextriclub.com", "Mid Sussex Tri Club password reset", sb.ToString());

                return Redirect("/login/");

            }
            return CurrentUmbracoPage();
        }
    }
}