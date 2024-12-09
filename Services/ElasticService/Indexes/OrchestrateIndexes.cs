using ElasticAPI.Services.ElasticService.Indexes.Create;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using ElasticAPI.Enums;
using System.Collections.Generic;
using ElasticAPI.Services.ElasticService.Indexes.Search;
using ElasticAPI.Models.CreationModels;
using ElasticAPI.Services.ElasticService.Indexes.Get;
using ElasticAPI.Models.Dto;

namespace ElasticAPI.Services.ElasticService.Indexes
{

    public class OrchestrateIndexes
    {
        private readonly GetTableProperties _getTableProperties;
        private readonly IConfiguration _configuration;
        private readonly GetPatients _getPatients;

        public OrchestrateIndexes(IConfiguration configuration)
        {
            _getTableProperties = new GetTableProperties();
            _configuration = configuration;
            _getPatients = new GetPatients();
        }


        public async Task<DataResponse> StartOrchestrationIndexes()
        {
            string connectionString = _configuration.GetConnectionString("SqlServer");
            string elasticUrl = _configuration.GetConnectionString("Elasticsearch");
            var elasticsearchService = new ElasticsearchService(elasticUrl);

            try
            {
                foreach (TableNames tableName in Enum.GetValues(typeof(TableNames)))
                {
                    bool isColumnExist = await _getTableProperties.CheckTableHasDataAsync(connectionString, tableName.ToString());
                    if (!isColumnExist)
                    {
                        return new DataResponse
                        {
                            Success = false,
                            Data = null,
                            Schema = null
                        };
                    }

                    var (data, schema) = await _getTableProperties.GetTableDataWithSchemaAsync(connectionString, tableName.ToString());
                    var tableData = new DataResponse
                    {
                        Success = true,
                        Data = data,
                        Schema = schema
                    };

                    bool isImported = await elasticsearchService.ImportDataToElasticsearch(tableName.ToString().ToLower(), tableData);
                    if (!isImported)
                    {
                        return new DataResponse
                        {
                            Success = false,
                            Data = data,
                            Schema = schema
                        };
                    }
                }

                return new DataResponse
                {
                    Success = true,
                    Data = null,
                    Schema = null
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Hata oluşan tablo: {Enum.GetName(typeof(TableNames), 0)}, Hata: {ex.Message}");
            }
        }

        public async Task<List<string>> SearchFromExaminations(string[] examinationIds)
        {
            string elasticUrl = _configuration.GetConnectionString("Elasticsearch");

            var searchIndex = new SearchIndex();
            var patientIds = await searchIndex.GetPatientIdsByExaminationIdsAsync(elasticUrl, examinationIds);

            if (patientIds.Count == 0)
            {
                throw new Exception("Aranan kayıt bulunamadı");
            }
            else
            {
                return patientIds;
            }

        }


        public async Task<List<PatientDto>> GetPatients()
        {
            string elasticUrl = _configuration.GetConnectionString("Elasticsearch");

            var patientList = await _getPatients.GetAllPatients(elasticUrl);

            if (patientList.Count == 0)
            {
                throw new Exception("Aranan kayıt bulunamadı");
            }
            else
            {
                return patientList;
            }

        }

        public async Task<List<PatientDto>> GetPatientsById(string[] patientIds)
        {
            string elasticUrl = _configuration.GetConnectionString("Elasticsearch");

            var patientList = await _getPatients.GetPatientsById(elasticUrl,patientIds);

            if (patientList.Count == 0)
            {
                throw new Exception("Aranan kayıt bulunamadı");
            }
            else
            {
                return patientList;
            }

        }
    }
}