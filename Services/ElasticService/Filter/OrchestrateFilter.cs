using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElasticAPI.Models.Dto;
using ElasticAPI.Services.ElasticService.Filter.Biopcy;
using ElasticAPI.Services.ElasticService.Filter.CoinfectionInformation;
using ElasticAPI.Services.ElasticService.Filter.HepatitBSerology;
using ElasticAPI.Services.ElasticService.Filter.NonInvasiveTests;
using ElasticAPI.Services.ElasticService.Filter.VaccineHistory;

namespace ElasticAPI.Services.ElasticService.Filter
{
    public interface IOrchestrateFilter
    {
        Task<string[]> StartOrchestrationFilter(FilterDto filterDto);
    }

    public class OrchestrateFilter : IOrchestrateFilter
    {
        private readonly List<(Func<FilterDto, Task<IEnumerable<string>>> Operation, Func<FilterDto, bool> Condition)> _filterOperations;

        public OrchestrateFilter(
            IBiopcyFilter biopcyFilter,
            IHepatitBSerologyFilter hepatitBSerologyFilter,
            ICoinfectionInformationFilter coinfectionInformationFilter,
            INonInvasiveTestsFilter nonInvasiveTestsFilter,
            IVaccineHistoryFilter vaccineHistoryFilter)
        {
            if (biopcyFilter == null) throw new ArgumentNullException(nameof(biopcyFilter));
            if (hepatitBSerologyFilter == null) throw new ArgumentNullException(nameof(hepatitBSerologyFilter));
            if (coinfectionInformationFilter == null) throw new ArgumentNullException(nameof(coinfectionInformationFilter));
            if (nonInvasiveTestsFilter == null) throw new ArgumentNullException(nameof(nonInvasiveTestsFilter));
            if (vaccineHistoryFilter == null) throw new ArgumentNullException(nameof(vaccineHistoryFilter));

            _filterOperations = new List<(Func<FilterDto, Task<IEnumerable<string>>>, Func<FilterDto, bool>)>
            {
                (
                    async (dto) => await biopcyFilter.FilterBiopcy(dto.Biopcy.Values),
                    (dto) => dto.Biopcy.Values.Count != 0
                ),
                (
                    async (dto) => await hepatitBSerologyFilter.FilterHepatitBSerology(dto.HepatitBSerology.Values),
                    (dto) => dto.HepatitBSerology.Values.Count != 0
                ),
                (
                    async (dto) => await coinfectionInformationFilter.FilterCoinfectionInformation(dto.CoinfectionInformation.Values),
                    (dto) => dto.CoinfectionInformation.Values.Count != 0
                ),
                (
                    async (dto) => await nonInvasiveTestsFilter.FilterNonInvasiveTests(dto.NonInvasiveTests.Values),
                    (dto) => dto.NonInvasiveTests.Values.Count != 0
                ),
                (
                    async (dto) => await vaccineHistoryFilter.FilterVaccineHistory(dto.VaccineHistory.Values),
                    (dto) => dto.VaccineHistory.Values.Count != 0
                )
            };
        }

        public async Task<string[]> StartOrchestrationFilter(FilterDto filterDto)
        {
            if (filterDto == null)
            {
                throw new ArgumentNullException(nameof(filterDto));
            }

            try
            {
                HashSet<string> result = null;

                foreach (var (operation, condition) in _filterOperations)
                {
                    if (condition(filterDto))
                    {
                        var filterResult = await operation(filterDto);

                        if (result == null)
                        {
                            result = new HashSet<string>(filterResult);
                        }
                        else
                        {
                            result.IntersectWith(filterResult);
                        }
                    }
                }

                return result?.ToArray() ?? Array.Empty<string>();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}