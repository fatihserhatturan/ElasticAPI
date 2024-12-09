using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ElasticAPI.Services.ElasticService.Indexes;
using System.Threading.Tasks;
using System;
using ElasticAPI.Services.ElasticService.Indexes.Search;
using Nest;
using ElasticAPI.Models.RequestModels;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using ElasticAPI.Services.LogicService;
using ElasticAPI.Services.ElasticService.Filter;
using System.Linq;
using ElasticAPI.Models.Dto;
using System.Collections.Generic;

namespace ElasticAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("AllowAll")]  
    public class ElasticMainController : ControllerBase
    {
        private readonly OrchestrateIndexes _orchestrateIndexes;
        private readonly ILogger<ElasticMainController> _logger;
        private readonly IFilterLogic _filterLogic;
        private readonly IOrchestrateFilter _orchestrateFilter;

        public ElasticMainController(OrchestrateIndexes orchestrateIndexes, ILogger<ElasticMainController> logger, IFilterLogic filterLogic, IOrchestrateFilter orchestrateFilter)
        {
            _orchestrateIndexes = orchestrateIndexes;
            _logger = logger;
            _filterLogic = filterLogic;
            _orchestrateFilter = orchestrateFilter;
        }

        [HttpGet("trigger")]
        public async Task<JsonResult> TriggerElastic()
        {
            try
            {
                var result = await _orchestrateIndexes.StartOrchestrationIndexes();
                return new JsonResult(result)
                {
                    StatusCode = 200,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TriggerElastic metodu sırasında bir hata oluştu");
                return new JsonResult(new { success = false, message = ex })
                {
                    StatusCode = 500,
                    ContentType = "application/json"
                };
            }
        }

        [HttpPost("search")]
        public async Task<JsonResult> SearchExamination([FromBody] SearchExaminationRequest request)
        {
            try
            {
                var patientIds = await _orchestrateIndexes.SearchFromExaminations(request.ExaminationIds);
                return new JsonResult(patientIds)
                {
                    StatusCode = 200,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchExamination metodu sırasında bir hata oluştu");
                return new JsonResult(new { success = false, message = ex })
                {
                    StatusCode = 500,
                    ContentType = "application/json"
                };
            }
        }

        [HttpGet("patients")]
        public async Task<JsonResult> PatientGet()
        {
            try
            {
                var result = await _orchestrateIndexes.GetPatients();
                return new JsonResult(result)
                {
                    StatusCode = 200,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PatientGet metodu sırasında bir hata oluştu");
                return new JsonResult(new { success = false, message = ex })
                {
                    StatusCode = 500,
                    ContentType = "application/json"
                };
            }
        }

        [HttpPost("filter")]
        public async Task<JsonResult> ReceiveFilter([FromBody] JsonElement filterData)
        {
            try
            {
                string jsonString = filterData.GetRawText();

                var filterLogic = new FilterLogic();

                var filterDto = await filterLogic.ConvertToFilterDto(jsonString);

                var examinationIds = await _orchestrateFilter.StartOrchestrationFilter(filterDto);
                List<PatientDto> patients;

                if (examinationIds.Length == 0)
                {
                     patients = await _orchestrateIndexes.GetPatients();
                }
                else
                {
                    var patientIds = await _orchestrateIndexes.SearchFromExaminations(examinationIds);
                    patients = await _orchestrateIndexes.GetPatientsById(patientIds.ToArray());
                }

                return new JsonResult(patients)
                {
                    StatusCode = 200,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message })
                {
                    StatusCode = 500,
                    ContentType = "application/json"
                };
            }
        }
    }
}