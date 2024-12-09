using ElasticAPI.Models.Dto;
using ElasticAPI.Services.ElasticService.Filter.Biopcy;
using ElasticAPI.Services.ElasticService.Indexes;
using ElasticAPI.Services.ElasticService.Indexes.Create;
using ElasticAPI.Services.ElasticService.Indexes.Get;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElasticAPI.Services.ElasticService.Filter
{
    public interface IOrchestrateFilter
    {
        Task<string[]> StartOrchestrationFilter(FilterDto filterDto);
    }

    public class OrchestrateFilter : IOrchestrateFilter
    {
        private readonly IBiopcyFilter _biopcyFilter;

        public OrchestrateFilter(IBiopcyFilter biopcyFilter)
        {
            _biopcyFilter = biopcyFilter ?? throw new ArgumentNullException(nameof(biopcyFilter));
        }

        public async Task<string[]> StartOrchestrationFilter(FilterDto filterDto)
        {
            if (filterDto == null)
            {
                throw new ArgumentNullException(nameof(filterDto));
            }

            try
            {
                var value = await _biopcyFilter.FilterBiopcy(filterDto.Biopcy.Values);

                Console.WriteLine("Filter orchestration completed successfully");

                return value;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}