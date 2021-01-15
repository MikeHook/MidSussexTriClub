using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace Mstc.Core.Providers
{
    public class ContentEventHandler
    {
        public void ContentService_Saving(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.SaveEventArgs<Umbraco.Core.Models.IContent> e)
        {
            /*
             *-	Add any slots which do not exist
                -	Check if any slots should be removed
	                - If slots have participants then cancel saving with message
	                - If slots are empty then remove the slots
                -	Update cost and max participants on existing slots
             */

            foreach (IContent entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias == "event")
                {

                }
            }   
        }

        public void ContentService_Deleting(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.DeleteEventArgs<Umbraco.Core.Models.IContent> e)
        {
            /*
             * -	Check if any slots should be removed
	                - If slots have participants then cancel deleting with message
	                - If slots are empty then remove the slots
             */
            //throw new NotImplementedException();
        }      
    }
}
