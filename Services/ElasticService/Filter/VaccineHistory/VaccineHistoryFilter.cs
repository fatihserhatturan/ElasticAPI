using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter.VaccineHistory
{
    public interface IVaccineHistoryFilter
    {
        Task<string[]> FilterVaccineHistory(List<string> vaccineTypes);
    }

    public class VaccineHistoryFilter : IVaccineHistoryFilter
    {
        private readonly IElasticClient _elasticClient;
        private readonly Dictionary<string, string> _fieldMappings;
        private const string DEFAULT_INDEX = "vaccinehistory";
        private const string EXAMINATION_ID_FIELD = "ExaminationId";

        public VaccineHistoryFilter(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var elasticUrl = configuration.GetConnectionString("Elasticsearch");
            if (string.IsNullOrWhiteSpace(elasticUrl))
                throw new InvalidOperationException("Elasticsearch connection string is not configured.");

            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex(DEFAULT_INDEX)
                .EnableDebugMode()
                .DisableDirectStreaming()
                .PrettyJson();

            _elasticClient = new ElasticClient(settings);

            _fieldMappings = new Dictionary<string, string>
            {
                { "hepatitB", "HepatitB" },
                { "hepatitA", "HepatitA" },
                { "hpv", "HPV" },
                { "zoster", "Zooster" },
                { "influenza", "Influenza" },
                { "rsv", "RSV" },
                { "pneumococcal", "Pneumococcus" },
                { "covid", "COVID" }
            };
        }

        public async Task<string[]> FilterVaccineHistory(List<string> vaccineTypes)
        {
            if (vaccineTypes == null || !vaccineTypes.Any())
            {
                return Array.Empty<string>();
            }

            try
            {
                var validVaccineTypes = vaccineTypes.Where(value => _fieldMappings.ContainsKey(value)).ToList();

                if (!validVaccineTypes.Any())
                {
                    return Array.Empty<string>();
                }

                var searchTasks = validVaccineTypes.Select(SearchExaminationIds);
                var searchResults = await Task.WhenAll(searchTasks);

                var finalResult = searchResults
                    .Where(result => result.Any())
                    .Aggregate((current, next) => current.Intersect(next).ToArray());

                return finalResult ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, "Error occurred while filtering vaccine history values");
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> SearchExaminationIds(string vaccineType)
        {
            try
            {
                if (!_fieldMappings.TryGetValue(vaccineType, out var field))
                {
                    return Array.Empty<string>();
                }

                var searchDescriptor = new SearchDescriptor<dynamic>()
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Match(mt => mt
                                    .Field(field)
                                    .Query("Var")
                                )
                            )
                        )
                    );

                var searchResponse = await _elasticClient.SearchAsync<dynamic>(searchDescriptor);

                if (!searchResponse.IsValid)
                {
                    // Elastic search hatası loglama
                    // _logger.LogError($"Elasticsearch error: {searchResponse.ServerError?.Error}");
                    return Array.Empty<string>();
                }

                return searchResponse.Hits
                    .Select(hit => hit.Source)
                    .OfType<IDictionary<string, object>>()
                    .Where(doc => doc.ContainsKey(EXAMINATION_ID_FIELD) && doc[EXAMINATION_ID_FIELD] != null)
                    .Select(doc => doc[EXAMINATION_ID_FIELD].ToString())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToArray();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, $"Error occurred while searching examination IDs for vaccine type: {vaccineType}");
                return Array.Empty<string>();
            }
        }
    }
}