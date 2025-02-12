using System.Linq;
using ClosedXML.Excel;

namespace CommissioningChecklistGenerator.Extensions
{
    public static class WorkbookExtensions
    {
        public static IXLWorksheet? GetWorksheetByName(this IXLWorkbook workbook, string name)
        {
            return workbook?.Worksheets?.OfType<IXLWorksheet>()?.FirstOrDefault(ws => ws.Name == name);
        }
    }
}
