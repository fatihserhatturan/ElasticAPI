using System.Collections.Generic;

namespace ElasticAPI.Models.Dto
{
    public class FilterDto
    {
        public Biopcy Biopcy { get; set; } = new();
        public HepatitBSerology HepatitBSerology { get; set; } = new();
        public NonInvasiveTests NonInvasiveTests { get; set; } = new();
        public LaboratoryFindings LaboratoryFindings { get; set; } = new();
        public VaccineHistory VaccineHistory { get; set; } = new();
        public CoinfectionInformation CoinfectionInformation { get; set; } = new();
        public Treatment Treatment { get; set; } = new();
        public SourceInformation SourceInformation { get; set; } = new();
    }

    public class Biopcy
    {
        public List<string> Values { get; set; } = new();
    }

    public class HepatitBSerology
    {
        public List<string> Values { get; set; } = new();
    }

    public class NonInvasiveTests
    {
        public List<string> Values { get; set; } = new();
    }

    public class LaboratoryFindings
    {
        public List<string> Values { get; set; } = new();
    }

    public class VaccineHistory
    {
        public List<string> Values { get; set; } = new();
    }

    public class CoinfectionInformation
    {
        public List<string> Values { get; set; } = new();
    }

    public class Treatment
    {
        public List<string> Values { get; set; } = new();
    }

    public class SourceInformation
    {
        public List<string> Values { get; set; } = new();
    }

    public static class FilterDtoExtensions
    {
        public static Dictionary<string, List<string>> ToDictionary(this FilterDto filterDto)
        {
            return new Dictionary<string, List<string>>
            {
                { nameof(filterDto.Biopcy), filterDto.Biopcy.Values },
                { nameof(filterDto.HepatitBSerology), filterDto.HepatitBSerology.Values },
                { nameof(filterDto.NonInvasiveTests), filterDto.NonInvasiveTests.Values },
                { nameof(filterDto.LaboratoryFindings), filterDto.LaboratoryFindings.Values },
                { nameof(filterDto.VaccineHistory), filterDto.VaccineHistory.Values },
                { nameof(filterDto.CoinfectionInformation), filterDto.CoinfectionInformation.Values },
                { nameof(filterDto.Treatment), filterDto.Treatment.Values },
                { nameof(filterDto.SourceInformation), filterDto.SourceInformation.Values }
            };
        }
    }
}