using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter.Biopcy
{
    public interface IBiopcyFilter
    {
        Task<string[]> FilterBiopcy(List<string> biopcyValues);
    }

    public class BiopcyFilter : IBiopcyFilter
    {
        private readonly IElasticClient _elasticClient;
        private readonly Dictionary<string, string[]> _fieldMappings;
        private const string DEFAULT_INDEX = "biopcy";
        private const string EXAMINATION_ID_FIELD = "ExaminationId";

        public BiopcyFilter(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var elasticUrl = configuration.GetConnectionString("Elasticsearch");
            if (string.IsNullOrWhiteSpace(elasticUrl))
                throw new InvalidOperationException("Elasticsearch connection string is not configured.");

            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex(DEFAULT_INDEX)
                .EnableDebugMode() // Development ortamında debug için kullanılabilir
                .DisableDirectStreaming() // Development ortamında debug için kullanılabilir
                .PrettyJson(); // Development ortamında debug için kullanılabilir

            _elasticClient = new ElasticClient(settings);

            _fieldMappings = new Dictionary<string, string[]>
            {
                { "hai", new[] { "HAI1", "HAI2", "HAI3" } },
                { "fibrosis", new[] { "Fibrosis1", "Fibrosis2", "Fibrosis3" } },
                { "portalArea", new[] { "PortalArea1", "PortalArea2", "PortalArea3" } }
            };
        }

        public async Task<string[]> FilterBiopcy(List<string> biopcyValues)
        {
            if (biopcyValues == null || !biopcyValues.Any())
            {
                return Array.Empty<string>();
            }

            try
            {
                var validBiopcyTypes = biopcyValues.Where(value => _fieldMappings.ContainsKey(value.ToLowerInvariant())).ToList();

                if (!validBiopcyTypes.Any())
                {
                    return Array.Empty<string>();
                }

                var searchTasks = validBiopcyTypes.Select(SearchExaminationIds);
                var searchResults = await Task.WhenAll(searchTasks);

                // İlk sonuç kümesini al ve diğerleriyle kesişimini bul
                var finalResult = searchResults
                    .Where(result => result.Any())
                    .Aggregate((current, next) => current.Intersect(next).ToArray());

                return finalResult ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Burada loglama yapılabilir
                // _logger.LogError(ex, "Error occurred while filtering biopcy values");
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> SearchExaminationIds(string biopcyType)
        {
            try
            {
                if (!_fieldMappings.TryGetValue(biopcyType.ToLowerInvariant(), out var fields))
                {
                    return Array.Empty<string>();
                }

                var searchDescriptor = new SearchDescriptor<dynamic>()
                    .Query(q => q
                        .Bool(b => b
                            .Should(
                                fields.Select(field =>
                                    q.Exists(e => e.Field(field)) as QueryContainer
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
                // _logger.LogError(ex, $"Error occurred while searching examination IDs for biopcy type: {biopcyType}");
                return Array.Empty<string>();
            }
        }
    }
}