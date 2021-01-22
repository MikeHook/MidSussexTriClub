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

        [Required, Display(Name = "Additional credits required*"), Range(1, 30)]
        public int TrainingCreditsToBuy { get; set; }
    }
}