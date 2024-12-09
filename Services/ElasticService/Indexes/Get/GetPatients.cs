using Nest;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using ElasticAPI.Models.Dto;

namespace ElasticAPI.Services.ElasticService.Indexes.Get
{


    public class GetPatients
    {
        public async Task<List<PatientDto>> GetAllPatients(string elasticUrl)
        {
            try
            {
                var settings = new ConnectionSettings(new Uri(elasticUrl))
                    .DefaultIndex("patients");
                var client = new ElasticClient(settings);

                var searchResponse = await client.SearchAsync<Dictionary<string, object>>(s => s
                    .Index("patients")
                    .Size(10000)
                    .Query(q => q.MatchAll())
                );

                if (!searchResponse.IsValid)
                {
                    throw new Exception($"Elasticsearch sorgusu başarısız: {searchResponse.ServerError?.Error}");
                }

                var patients = new List<PatientDto>();
                foreach (var hit in searchResponse.Hits)
                {
                    // Önce tüm veriyi JSON'a çevir
                    var fullJson = JsonConvert.SerializeObject(hit.Source);
                    // Sonra JSON'ı PatientDto'ya deserialize et (bu otomatik olarak sadece istenen alanları alacak)
                    var patientDto = JsonConvert.DeserializeObject<PatientDto>(fullJson);
                    patients.Add(patientDto);
                }

                return patients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Elasticsearch sorgusu sırasında hata oluştu: {ex.Message}", ex);
            }
        }

        public async Task<List<PatientDto>> GetPatientsById(string elasticUrl, string[] patientIds)
        {
            try
            {
                var settings = new ConnectionSettings(new Uri(elasticUrl))
                    .DefaultIndex("patients");
                var client = new ElasticClient(settings);

                var searchResponse = await client.SearchAsync<Dictionary<string, object>>(s => s
                    .Index("patients")
                    .Size(10000)
                    .Query(q => q
                        .Terms(t => t
                            .Field("Id")
                            .Terms(patientIds)
                        )
                    )
                );

                if (!searchResponse.IsValid)
                {
                    throw new Exception($"Elasticsearch sorgusu başarısız: {searchResponse.ServerError?.Error}");
                }

                var patients = new List<PatientDto>();
                foreach (var hit in searchResponse.Hits)
                {
                    // Önce tüm veriyi JSON'a çevir
                    var fullJson = JsonConvert.SerializeObject(hit.Source);
                    // Sonra JSON'ı PatientDto'ya deserialize et
                    var patientDto = JsonConvert.DeserializeObject<PatientDto>(fullJson);
                    patients.Add(patientDto);
                }

                return patients;
            }
            catch (Exception ex)
            {
                throw new Exception($"Elasticsearch sorgusu sırasında hata oluştu: {ex.Message}", ex);
            }
        }

    }
}