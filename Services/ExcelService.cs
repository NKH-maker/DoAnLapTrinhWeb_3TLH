using ClosedXML.Excel;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? xu?t Excel
    /// </summary>
    public interface IExcelService
    {
        byte[] ExportToExcel<T>(List<T> data, string sheetName);
        Task<byte[]> ExportOrdersToExcelAsync(List<dynamic> orders);
    }

    public class ExcelService : IExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
        }

        public byte[] ExportToExcel<T>(List<T> data, string sheetName)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);
                    
                    // Get properties
                    var properties = typeof(T).GetProperties();
                    
                    // Add headers
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    }
                    
                    // Add data
                    for (int row = 0; row < data.Count; row++)
                    {
                        for (int col = 0; col < properties.Length; col++)
                        {
                            var value = properties[col].GetValue(data[row]);
                            worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
                        }
                    }
                    
                    // Format
                    worksheet.Columns().AdjustToContents();
                    
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting to Excel: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> ExportOrdersToExcelAsync(List<dynamic> orders)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Orders");
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "Order ID";
                        worksheet.Cell(1, 2).Value = "Customer";
                        worksheet.Cell(1, 3).Value = "Date";
                        worksheet.Cell(1, 4).Value = "Total";
                        worksheet.Cell(1, 5).Value = "Status";
                        
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            return stream.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error exporting orders to Excel: {ex.Message}");
                    throw;
                }
            });
        }
    }
}
