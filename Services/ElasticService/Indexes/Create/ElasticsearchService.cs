using Nest;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using ElasticAPI.Models.CreationModels;

public class ElasticsearchService
{
    private readonly IElasticClient _elasticClient;

    public ElasticsearchService(string elasticsearchUrl)
    {
        var connectionSettings = new ConnectionSettings(new Uri(elasticsearchUrl));
        _elasticClient = new ElasticClient(connectionSettings);
    }

    public async Task<bool> ImportDataToElasticsearch(string indexName, DataResponse patientData)
    {
        try
        {
            var indexExists = await _elasticClient.Indices.ExistsAsync(indexName);

            if (indexExists.Exists)
            {
                var deleteResponse = await _elasticClient.Indices.DeleteAsync(indexName);
                if (!deleteResponse.IsValid)
                {
                    return false;
                }
            }

            var createResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1)
                )
            );

            if (!createResponse.IsValid)
            {
                return false;
            }

            var bulkDescriptor = new BulkDescriptor();
            foreach (var record in patientData.Data)
            {
                var json = JsonConvert.SerializeObject(record);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                bulkDescriptor.Index<Dictionary<string, object>>(i => i
                    .Index(indexName)
                    .Document(dictionary)
                    .Id(dictionary["Id"].ToString())
                );
            }

            var response = await _elasticClient.BulkAsync(bulkDescriptor);

            if (!response.Errors)
            {
                return true;
            }
            else
            {
                return response.IsValid && !response.Errors;
            }
            
        }
        catch
        {
            return false;
        }
    }
}