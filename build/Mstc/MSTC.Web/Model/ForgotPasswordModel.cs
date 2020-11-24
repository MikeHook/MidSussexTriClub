using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;

namespace MSTC.Web.Model
{
    public class ForgotPasswordModel
    {
        [Display(Name = "Email")]
        [Required]
        public string Email { get; set; }
    }
}