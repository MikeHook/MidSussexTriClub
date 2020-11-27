using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstc.Core.Validation
{
    public class DateOver16Validator : ValidationAttribute
    {
        public override bool IsValid(object date)
        {
            if (date == null || !(date is DateTime))
            {
                return false;
            }
            DateTime dateOfBirth = (DateTime)date;
            return dateOfBirth < DateTime.Now.AddYears(-16);
        }
    }
}
