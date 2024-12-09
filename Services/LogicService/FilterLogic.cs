using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ElasticAPI.Models.Dto;

namespace ElasticAPI.Services.LogicService
{
    public interface IFilterLogic
    {
        Task<FilterDto> ConvertToFilterDto(string jsonData);
    }

    public class FilterLogic : IFilterLogic
    {
        public async Task<FilterDto> ConvertToFilterDto(string jsonData)
        {
            try
            {

                var rawData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonData);

                var filterDto = new FilterDto();

                foreach (var item in rawData)
                {
                    switch (item.Key.ToLower())
                    {
                        case "biopcy":
                            filterDto.Biopcy.Values = item.Value;
                            break;
                        case "hepatitbserology":
                            filterDto.HepatitBSerology.Values = item.Value;
                            break;
                        case "noninvasivetests":
                            filterDto.NonInvasiveTests.Values = item.Value;
                            break;
                        case "laboratoryfindings":
                            filterDto.LaboratoryFindings.Values = item.Value;
                            break;
                        case "vaccinehistory":
                            filterDto.VaccineHistory.Values = item.Value;
                            break;
                        case "coinfectioninformation":
                            filterDto.CoinfectionInformation.Values = item.Value;
                            break;
                        case "treatment":
                            filterDto.Treatment.Values = item.Value;
                            break;
                        case "sourceinformation":
                            filterDto.SourceInformation.Values = item.Value;
                            break;
                    }
                }

                return filterDto;
            }
            catch (Exception ex)
            {
                throw new Exception("FilterDto dönüşümü sırasında hata oluştu", ex);
            }
        }

        public Dictionary<string, List<string>> ConvertToDictionary(FilterDto filterDto)
        {
            return filterDto.ToDictionary();
        }
    }
}