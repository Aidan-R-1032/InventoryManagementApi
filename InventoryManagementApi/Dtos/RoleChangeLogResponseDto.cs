namespace InventoryManagementApi.Dtos
{
    public class RoleChangeLogResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int ResultsReturned { get; set; }
        public IEnumerable<RoleChangeLogViewerDto> Logs { get; set; }
            = Enumerable.Empty<RoleChangeLogViewerDto>();
    }
}