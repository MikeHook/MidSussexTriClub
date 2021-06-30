using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model.Partials
{
    public class TrainingCreditsModel
    {
        public int CurrentTrainingCredits { get; set; }

        [Required, Display(Name = "Buy credits"), Range(1, 50)]
        public int TrainingCreditsToBuy { get; set; }

        public string EventBookingPageUrl { get; set; }

        public bool IsTrainingCreditsEnabled { get; set; }
    }
}