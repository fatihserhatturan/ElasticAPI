using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter.HepatitBSerology
{
    public interface IHepatitBSerologyFilter
    {
        Task<string[]> FilterHepatitBSerology(List<string> serologyTypes);
    }

    public class HepatitBSerologyFilter : IHepatitBSerologyFilter
    {
        private readonly IElasticClient _elasticClient;
        private readonly Dictionary<string, string[]> _fieldMappings;
        private const string DEFAULT_INDEX = "hepatitbserology";
        private const string EXAMINATION_ID_FIELD = "ExaminationId";

        public HepatitBSerologyFilter(IConfiguration configuration)
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

            _fieldMappings = new Dictionary<string, string[]>
            {
                { "hbsag", new[] { "HbsAGFirstYear", "HbsAG1", "HbsAG2", "HbsAG3", "HbsAG4", "HbsAG5", "HbsAG6", "HbsAG7", "HbsAG8", "HbsAG9", "HbsAG10" } },
                { "hbeag", new[] { "HbeAGFirstYear", "HbeAG1", "HbeAG2", "HbeAG3", "HbeAG4", "HbeAG5", "HbeAG6", "HbeAG7", "HbeAG8", "HbeAG9", "HbeAG10" } },
                { "antiHbe", new[] { "AntiHbeFirstYear", "AntiHbe1", "AntiHbe2", "AntiHbe3", "AntiHbe4", "AntiHbe5", "AntiHbe6", "AntiHbe7", "AntiHbe8", "AntiHbe9", "AntiHbe10" } },
                { "antiHbcIgm", new[] { "AntiHbcIgMFirstYear", "AntiHbcIgM1", "AntiHbcIgM2", "AntiHbcIgM3", "AntiHbcIgM4", "AntiHbcIgM5", "AntiHbcIgM6", "AntiHbcIgM7", "AntiHbcIgM8", "AntiHbcIgM9", "AntiHbcIgM10" } },
                { "antiHbcIgg", new[] { "AntiHbcIgGFirstYear", "AntiHbcIgG1", "AntiHbcIgG2", "AntiHbcIgG3", "AntiHbcIgG4", "AntiHbcIgG5", "AntiHbcIgG6", "AntiHbcIgG7", "AntiHbcIgG8", "AntiHbcIgG9", "AntiHbcIgG10" } },
                { "antiHbs", new[] { "AntiHbsFirstYear", "AntiHbs1", "AntiHbs2", "AntiHbs3", "AntiHbs4", "AntiHbs5", "AntiHbs6", "AntiHbs7", "AntiHbs8", "AntiHbs9", "AntiHbs10" } },
                { "hbvDna", new[] { "HBVDNAFirstYear", "HBVDNA1", "HBVDNA2", "HBVDNA3", "HBVDNA4", "HBVDNA5", "HBVDNA6", "HBVDNA7", "HBVDNA8", "HBVDNA9", "HBVDNA10" } },
                { "hdvRna", new[] { "HDVRNAFirstYear", "HDVRNA1", "HDVRNA2", "HDVRNA3", "HDVRNA4", "HDVRNA5", "HDVRNA6", "HDVRNA7", "HDVRNA8", "HDVRNA9", "HDVRNA10" } }
            };
        }

        public async Task<string[]> FilterHepatitBSerology(List<string> serologyTypes)
        {
            if (serologyTypes == null || !serologyTypes.Any())
            {
                return Array.Empty<string>();
            }

            try
            {
                var validSerologyTypes = serologyTypes.Where(value => _fieldMappings.ContainsKey(value.ToLowerInvariant())).ToList();

                if (!validSerologyTypes.Any())
                {
                    return Array.Empty<string>();
                }

                var searchTasks = validSerologyTypes.Select(SearchExaminationIds);
                var searchResults = await Task.WhenAll(searchTasks);

                var finalResult = searchResults
                    .Where(result => result.Any())
                    .Aggregate((current, next) => current.Intersect(next).ToArray());

                return finalResult ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, "Error occurred while filtering hepatit B serology values");
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> SearchExaminationIds(string serologyType)
        {
            try
            {
                if (!_fieldMappings.TryGetValue(serologyType.ToLowerInvariant(), out var fields))
                {
                    return Array.Empty<string>();
                }

                var searchDescriptor = new SearchDescriptor<dynamic>()
                    .Query(q => q
                        .Bool(b => b
                            .Should(
                                fields.Select(field =>
                                    q.Bool(innerBool => innerBool
                                        .Must(
                                            m => m.Exists(e => e.Field(field)),
                                            m => m.Match(mt => mt.Field(field).Query("1"))
                                        )
                                    ) as QueryContainer
                                ).ToArray()
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
                // _logger.LogError(ex, $"Error occurred while searching examination IDs for serology type: {serologyType}");
                return Array.Empty<string>();
            }
        }
    }
}