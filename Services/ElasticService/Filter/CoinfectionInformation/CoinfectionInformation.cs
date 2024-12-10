using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter.CoinfectionInformation
{
    public interface ICoinfectionInformationFilter
    {
        Task<string[]> FilterCoinfectionInformation(List<string> coinfectionTypes);
    }

    public class CoinfectionInformationFilter : ICoinfectionInformationFilter
    {
        private readonly IElasticClient _elasticClient;
        private readonly Dictionary<string, string> _fieldMappings;
        private const string DEFAULT_INDEX = "coinfectioninformatio";
        private const string EXAMINATION_ID_FIELD = "ExaminationId";

        public CoinfectionInformationFilter(IConfiguration configuration)
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
                { "antiHcv", "AntiHCV" },
                { "hcvRna", "HCVRNA" },
                { "antiHiv", "AntiHiv" },
                { "antiHdvIgm", "AntiHDVLgm" },
                { "antiHdvIgg", "AntiHDVLgg" },
                { "hdvRna", "HDVRNA" },
                { "antiHavIgm", "AntiHAVLgM" },
                { "vdrl", "VDRL" },
                { "tpha", "TPHA" },
                { "cmvIgm", "CMVLgM" },
                { "cmvIgg", "CMVLgG" }
            };
        }

        public async Task<string[]> FilterCoinfectionInformation(List<string> coinfectionTypes)
        {
            if (coinfectionTypes == null || !coinfectionTypes.Any())
            {
                return Array.Empty<string>();
            }

            try
            {
                var validCoinfectionTypes = coinfectionTypes.Where(value => _fieldMappings.ContainsKey(value)).ToList();

                if (!validCoinfectionTypes.Any())
                {
                    return Array.Empty<string>();
                }

                var searchTasks = validCoinfectionTypes.Select(SearchExaminationIds);
                var searchResults = await Task.WhenAll(searchTasks);

                var finalResult = searchResults
                    .Where(result => result.Any())
                    .Aggregate((current, next) => current.Intersect(next).ToArray());

                return finalResult ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, "Error occurred while filtering coinfection information values");
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> SearchExaminationIds(string coinfectionType)
        {
            try
            {
                if (!_fieldMappings.TryGetValue(coinfectionType, out var field))
                {
                    return Array.Empty<string>();
                }

                var searchDescriptor = new SearchDescriptor<dynamic>()
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Match(mt => mt
                                    .Field(field)
                                    .Query("Pozitif")
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
                // _logger.LogError(ex, $"Error occurred while searching examination IDs for coinfection type: {coinfectionType}");
                return Array.Empty<string>();
            }
        }
    }
}