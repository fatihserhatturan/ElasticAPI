using System.Collections.Generic;

namespace ElasticAPI.Models.CreationModels
{

    public class DataResponse
    {
        public bool Success { get; set; }
        public IEnumerable<dynamic> Data { get; set; }
        public IEnumerable<ColumnSchema> Schema { get; set; }
    }

}
