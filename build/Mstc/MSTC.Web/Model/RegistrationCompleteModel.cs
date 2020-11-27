using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;

namespace MSTC.Web.Model
{
    public class RegistrationCompleteModel
    {
        public bool PromptForConfirmation { get; set; }
        public string PaymentDescription { get; set; }
        public string Cost { get; set; }
        public bool IsRegistered { get; set; }
    }
}