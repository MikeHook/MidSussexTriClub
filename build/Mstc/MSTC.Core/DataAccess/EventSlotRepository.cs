using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Umbraco.Web;

namespace Mstc.Core.DataAccess
{
    public interface IEventSlotRepository
    {
        EventSlot Create(EventSlot eventSlot);
        EventSlot GetById(int id);
        IEnumerable<EventSlot> GetAll(bool futureEventsOnly, List<EventType> eventTypes);
        void Delete(int id);
        void Update(EventSlot slot);
    }

    public class EventSlotRepository : IEventSlotRepository
    {
        string _baseGet = @"SELECT [Id]
                                      ,[EventPageId]
                                      ,[EventTypeId]
                                      ,[Date]
                                      ,[Cost]
                                      ,[MaxParticipants]
                                      ,[Distances]
                                      ,[IndemnityWaiverDocumentLink]
                                      ,[CovidDocumentLink]
                                      ,[IsGuestEvent]
                                  FROM [dbo].[EventSlot] ";

        private readonly IDataConnection _dataConnection;
        private readonly IEventParticipantRepository _eventParticipantRepository;  

        public EventSlotRepository(IDataConnection dataConnection)
        {
            _dataConnection = dataConnection; 
            _eventParticipantRepository = new EventParticipantRepository(_dataConnection);    
        }

        public EventSlot GetById(int id)
        {
            string query = $"{_baseGet} Where [Id] = @id";

            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                var eventSlot = connection.QueryFirstOrDefault<EventSlot>(query, new { @id = id });
                if (eventSlot != null)
                {
                    eventSlot.EventParticipants = _eventParticipantRepository.GetByEventSlot(connection, eventSlot.Id).ToList();
                }
                return eventSlot;
            }
        }

        public IEnumerable<EventSlot> GetAll(bool futureEventsOnly, List<EventType> eventTypes)
        {
            string query = $"{_baseGet} {(futureEventsOnly ? "Where [Date] > @now" : "")} order By Date";

            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                var eventSlots = connection.Query<EventSlot>(query, new { now = DateTime.Now }).ToList();
                var eventParticipants = _eventParticipantRepository.GetAll(connection).ToList();                
                foreach (var eventSlot in eventSlots)
                {
                    eventSlot.EventParticipants = eventParticipants.Where(ep => ep.EventSlotId == eventSlot.Id).ToList();
                    eventSlot.EventTypeName = eventTypes.SingleOrDefault(et => et.Id == eventSlot.EventTypeId)?.Name;
                }
                return eventSlots;
            }
        }

        public EventSlot Create(EventSlot eventSlot)
        {
            string query = @"INSERT INTO [dbo].[EventSlot]            
                                           ([EventTypeId]
                                           ,[EventPageId]
                                           ,[Date]
                                           ,[Cost]
                                           ,[MaxParticipants]
                                           ,[Distances]
                                           ,[IndemnityWaiverDocumentLink]
                                           ,[CovidDocumentLink]
                                           ,[IsGuestEvent])
                                        OUTPUT Inserted.Id
                                        VALUES
                                           (@EventTypeId
                                           ,@EventPageId
                                           ,@Date
                                           ,@Cost
                                           ,@MaxParticipants
                                           ,@Distances
                                           ,@IndemnityWaiverDocumentLink
                                           ,@CovidDocumentLink
                                           ,@IsGuestEvent)";


            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                eventSlot.Id = connection.Query<int>(query, eventSlot).Single();
            }

            return eventSlot;
        }        

        public void Update(EventSlot slot)
        {
            string query = @"UPDATE [dbo].[EventSlot]
                                    SET  [Cost] = @Cost
                                        ,[MaxParticipants] = @MaxParticipants
                                        ,[Distances] = @Distances
                                        ,[IndemnityWaiverDocumentLink] = @IndemnityWaiverDocumentLink
                                        ,[CovidDocumentLink] = @CovidDocumentLink
                                        ,[IsGuestEvent] = @IsGuestEvent
                                    WHERE Id = @Id";
            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                connection.Execute(query, new
                {
                    Id = slot.Id,
                    Cost = slot.Cost,
                    MaxParticipants = slot.MaxParticipants,
                    Distances = slot.Distances,
                    IndemnityWaiverDocumentLink = slot.IndemnityWaiverDocumentLink,
                    CovidDocumentLink = slot.CovidDocumentLink,
                    IsGuestEvent = slot.IsGuestEvent
                });
            }
        }

        public void Delete(int id)
        {
            string query = @"DELETE FROM [dbo].[EventSlot] WHERE Id = @Id";
            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                connection.Execute(query, new { Id = id });
            }
        }        
    }
}
