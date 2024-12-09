using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Indexes.Search
{
    public class SearchIndex
    {
        public async Task<List<string>> GetPatientIdsByExaminationIdsAsync(string elasticUrl, string[] examinationIds)
        {
            try
            {
                var settings = new ConnectionSettings(new Uri(elasticUrl))
                    .DefaultIndex("examinations");

                var client = new ElasticClient(settings);

                var searchResponse = await client.SearchAsync<Dictionary<string, object>>(s => s
                    .Query(q => q
                        .Terms(t => t
                            .Field("Id")
                            .Terms(examinationIds)
                        )
                    )
                    .Source(sf => sf
                        .Includes(i => i
                            .Field("PatientId")
                        )
                    )
                    .Size(examinationIds.Length)
                );

                if (!searchResponse.IsValid)
                {
                    throw new Exception($"Elasticsearch sorgusu başarısız: {searchResponse.DebugInformation}");
                }


                var patientIds = searchResponse.Documents
                    .Where(doc => doc.ContainsKey("PatientId"))
                    .Select(doc => doc["PatientId"]?.ToString())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                return patientIds;
            }
            catch (Exception ex)
            {
                throw new Exception($"Elasticsearch sorgusu sırasında hata oluştu: {ex.Message}", ex);
            }
        }
    }
}