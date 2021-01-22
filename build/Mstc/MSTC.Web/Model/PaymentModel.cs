using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Web.PublishedContentModels;

namespace MSTC.Web.Model
{
    public class PaymentModel
    {
        public PaymentModel()
        {

        }

        public bool HasPaymentDetails { get; set; }
        public bool PaymentConfirmed { get; set; }
        public string PaymentDescription { get; set; }
        public decimal Cost { get; set; }

        public bool ShowPaymentFailed { get; set; }
        public bool ShowSwimSubsConfirmation { get; set; }
        public bool ShowRenewed { get; set; }
        public bool ShowCreditsConfirmation { get; set; }
    }
}