﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mstc.Core.Domain;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Mstc.Core.Providers
{
    public class DataTypeProvider
    {
        private readonly IDataTypeService _dataTypeService;

        public DataTypeProvider(IDataTypeService dataTypeService)
        {
            _dataTypeService = dataTypeService;
        }

        public List<EventType> GetEventTypes()
        {
            var eventTypesCollection = GetDataTypeOptions("Event - Event Type - Dropdown");
            return eventTypesCollection.PreValuesAsDictionary.Where(e => e.Value.Value != "0")
                .Select(e => new EventType() { Id = e.Value.Id, Name = e.Value.Value }).ToList();
        }

        public PreValueCollection GetDataTypeOptions(string dataTypeName)
        {
            var dataType = _dataTypeService.GetDataTypeDefinitionByName(dataTypeName);
            if (dataType == null)
            {
                return null;
            }
            return _dataTypeService.GetPreValuesCollectionByDataTypeId(dataType.Id);
        }

        public int? GetDropDownId(string dataTypeName, string val)
        {
            var dataType = _dataTypeService.GetDataTypeDefinitionByName(dataTypeName);
            if (dataType == null)
            {
                return null;
            }
            PreValue preValue = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataType.Id).PreValuesAsArray.FirstOrDefault(d => d.Value == val);
            return preValue != null ? preValue.Id : (int?)null;
        }
    }    
}
