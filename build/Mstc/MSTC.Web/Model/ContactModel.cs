using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Mstc.Core.Domain;
using Umbraco.Core.Models;

namespace MSTC.Web.Model
{
    public class ContactModel
    {
        [Display(Name = "Name")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Email")]
        [Required]
        public string Email { get; set; }

        [Display(Name = "Topic")]
        [Required]
        public TopicEnum Topic { get; set; }

        [Display(Name = "Message")]
        [Required]
        public string Message { get; set; }


    }
}