using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter.SourceInformation
{
    public interface ISourceInformationFilter
    {
        Task<string[]> FilterSourceInformation(List<string> sourceTypes);
    }

    public class SourceInformationFilter : ISourceInformationFilter
    {
        private readonly IElasticClient _elasticClient;
        private readonly Dictionary<string, string> _fieldMappings;
        private const string DEFAULT_INDEX = "sourceinformation";
        private const string EXAMINATION_ID_FIELD = "ExaminationId";

        public SourceInformationFilter(IConfiguration configuration)
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
                { "HaveFamilyHepatitB", "HaveFamilyHepatitB" },
                { "HaveFamilyCirrhosis", "HaveFamilyCirrhosis" },
                { "HaveFamilyLiverCancer", "HaveFamilyLiverCancer" },
                { "HaveFamilySurgeryBlood", "HaveFamilySurgeryBlood" }
            };
        }

        public async Task<string[]> FilterSourceInformation(List<string> sourceTypes)
        {
            if (sourceTypes == null || !sourceTypes.Any())
            {
                return Array.Empty<string>();
            }

            try
            {
                var validSourceTypes = sourceTypes.Where(value => _fieldMappings.ContainsKey(value)).ToList();

                if (!validSourceTypes.Any())
                {
                    return Array.Empty<string>();
                }

                var searchTasks = validSourceTypes.Select(SearchExaminationIds);
                var searchResults = await Task.WhenAll(searchTasks);

                var finalResult = searchResults
                    .Where(result => result.Any())
                    .Aggregate((current, next) => current.Intersect(next).ToArray());

                return finalResult ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, "Error occurred while filtering source information values");
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> SearchExaminationIds(string sourceType)
        {
            try
            {
                if (!_fieldMappings.TryGetValue(sourceType, out var field))
                {
                    return Array.Empty<string>();
                }

                var searchDescriptor = new SearchDescriptor<dynamic>()
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Match(mt => mt
                                    .Field(field)
                                    .Query("Evet")
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
                // _logger.LogError(ex, $"Error occurred while searching examination IDs for source type: {sourceType}");
                return Array.Empty<string>();
            }
        }
    }
}