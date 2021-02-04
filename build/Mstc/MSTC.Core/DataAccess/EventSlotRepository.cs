using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Mstc.Core.Domain;

namespace Mstc.Core.DataAccess
{
    public interface IEventSlotRepository
    {
        EventSlot Create(EventSlot eventSlot);
        EventSlot GetById(int id);
        IEnumerable<EventSlot> GetAll(bool futureEventsOnly);
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

        public IEnumerable<EventSlot> GetAll(bool futureEventsOnly)
        {
            string query = $"{_baseGet} {(futureEventsOnly ? "Where [Date] > @now" : "")} order By Date";

            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                var eventSlots = connection.Query<EventSlot>(query, new { now = DateTime.Now }).ToList();
                var eventParticipants = _eventParticipantRepository.GetAll(connection).ToList();
                foreach(var eventSlot in eventSlots)
                {
                    eventSlot.EventParticipants = eventParticipants.Where(ep => ep.EventSlotId == eventSlot.Id).ToList();
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
                                           ,[MaxParticipants])
                                        OUTPUT Inserted.Id
                                        VALUES
                                           (@EventTypeId
                                           ,@EventPageId
                                           ,@Date
                                           ,@Cost
                                           ,@MaxParticipants)";


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
                                    WHERE Id = @Id";
            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                connection.Execute(query, new { Id = slot.Id, Cost = slot.Cost, MaxParticipants = slot.MaxParticipants });
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
