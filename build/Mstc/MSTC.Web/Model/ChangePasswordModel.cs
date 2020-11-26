using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;

namespace MSTC.Web.Model
{
    public class ChangePasswordModel
    {
        [Display(Name = "Old Password")]
        [Required]
        public string OldPassword { get; set; }

        [Display(Name = "New Password")]
        [Required]
        public string NewPassword { get; set; }
    }
}