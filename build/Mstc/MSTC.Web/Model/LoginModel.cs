using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;

namespace MSTC.Web.Model
{
    public class LoginModel
    {
        [Display(Name = "Email")]
        [Required]
        public string Username { get; set; }

        [Display(Name = "Password")]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public IPublishedContent CurrentPage { get; set; }
    }
}