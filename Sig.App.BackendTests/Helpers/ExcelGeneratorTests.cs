using System.IO;
using System.Linq;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;
using Sig.App.Backend.Helpers;

namespace Sig.App.BackendTests.Helpers
{
    public class ExcelGeneratorTests
    {
        [Fact]
        public void CanGenerateComplexDocument()
        {
            var result = GenerateComplexDocument();

            var xl = new XLWorkbook(result);

#if DEBUG
            // Saves to /bin/Debug folder
            xl.SaveAs("CanGenerateComplexDocument.xlsx");
#endif

            xl.Worksheets.Count.Should().Be(2);
            xl.Worksheet("Items").RowsUsed().Count().Should().Be(6); // 1 heading row + 4 data rows + 1 footer row
            xl.Worksheet("Items").Cell(3, 4).Value.Should().Be("The second item");
            xl.Worksheet("Items").Cell(1, 3).Style.Fill.BackgroundColor.Should().Be(XLColor.Red);
            xl.Worksheet("Items").Cell(6, 2).Value.Should().Be(10); // 1+2+3+4
            xl.Worksheet("Custom worksheet").Cell(2, 2).Value.Should().Be("This is a custom worksheet!");
        }

        [Fact]
        public void CanStartFromExistingDocument()
        {
            var startingDocument = GenerateSimpleDocument();
            var generator = new ExcelGenerator(startingDocument);
            generator.AddCustomWorksheet("Test", ws => ws.Cell(1, 1).Value = "Test");
            var finalDocument = generator.Render();


            var xl = new XLWorkbook(finalDocument);

            xl.Worksheets.Count.Should().Be(2);
            xl.Worksheet("Items").Cell(2, 1).Value.Should().Be("Item 1");
            xl.Worksheet("Test").Cell(1, 1).Value.Should().Be("Test");
        }

        private static Stream GenerateSimpleDocument()
        {
            var data = new[]
            {
                new { Name = "Item 1" },
                new { Name = "Item 2" },
                new { Name = "Item 3" },
                new { Name = "Item 4" },
            };

            var generator = new ExcelGenerator();

            generator.AddDataWorksheet("Items", data)
                .Column("Name", x => x.Name);
            
            return generator.Render();
        }
        
        private static Stream GenerateComplexDocument()
        {
            var data = new[]
            {
                new { Name = "Item 1", Description = "The first item", Value = 1 },
                new { Name = "Item 2", Description = "The second item", Value = 2 },
                new { Name = "Item 3", Description = "The third item", Value = 3 },
                new { Name = "Item 4", Description = "The fourth item", Value = 4 },
            };

            var generator = new ExcelGenerator();

            generator.AddDataWorksheet("Items", data)
                // Basic columns
                .Column("Name", x => x.Name)
                .Column("Value", x => x.Value)
                // Custom heading
                .Column(
                    x =>
                    {
                        x.Value = "Name Custom";
                        x.Style.Fill.BackgroundColor = XLColor.Red;
                    },
                    x => x.Name
                )
                // Custom cell
                .Column(
                    "Description",
                    (item, cell) =>
                    {
                        cell.Value = item.Description;
                        cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                    }
                )
                // Custom heading and cell
                .Column(
                    x =>
                    {
                        x.Value = "Description Custom";
                        x.Style.Fill.BackgroundColor = XLColor.Blue;
                    },
                    (item, cell) =>
                    {
                        cell.Value = item.Description;
                        cell.Style.Fill.BackgroundColor = cell.WorksheetRow().RowNumber() % 2 == 0
                            ? XLColor.Green
                            : XLColor.Aqua;
                    }
                )
                // Row added after the data
                .AddFooterRow(row =>
                {
                    row.Cell(1).Value = "Sum of values:";
                    row.Cell(2).SetFormulaA1($"=SUM(B2:B{row.RowNumber() - 1})");
                });

            generator.AddCustomWorksheet("Custom worksheet", worksheet =>
            {
                worksheet.Cell(2, 2).Value = "This is a custom worksheet!";
                worksheet.Column(2).AdjustToContents();
            });

            return generator.Render();
        }
    }
}