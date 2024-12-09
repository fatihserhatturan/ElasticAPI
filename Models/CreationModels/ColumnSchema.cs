namespace ElasticAPI.Models.CreationModels
{
    public class ColumnSchema
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public string IsNullable { get; set; }
    }
}
