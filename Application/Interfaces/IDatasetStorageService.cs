using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IDatasetStorageService
{
    Task WriteDatasetFileAsync(Guid datasetId, IEnumerable<string> dataSources, string category, DateTime? dateRangeStart, DateTime? dateRangeEnd);
}
