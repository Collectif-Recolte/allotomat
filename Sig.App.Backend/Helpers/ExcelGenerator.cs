using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace Sig.App.Backend.Helpers
{
    public partial class ExcelGenerator
    {
        private readonly XLWorkbook workbook;
        private readonly List<Worksheet> worksheets = new();

        /// <summary>
        /// Creates an ExcelGenerator instance that starts with a blank document.
        /// </summary>
        public ExcelGenerator()
        {
            workbook = new XLWorkbook();
        }
        
        /// <summary>
        /// Creates an ExcelGenerator instance that can add worksheets to an existing document.
        /// </summary>
        /// <param name="stream">A stream that will be loaded into an XLWorkbook object.</param>
        public ExcelGenerator(Stream stream)
        {
            workbook = new XLWorkbook(stream);
        }

        /// <summary>
        /// Creates an ExcelGenerator instance that can add worksheets to an existing document.
        /// </summary>
        /// <param name="workbook">The workbook to start from</param>
        public ExcelGenerator(XLWorkbook workbook)
        {
            this.workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
        }

        public Stream Render()
        {
            foreach (var worksheet in worksheets)
            {
                var xlWorksheet = workbook.AddWorksheet(worksheet.Name);
                xlWorksheet.ColumnWidth = 25;
                worksheet.Render(xlWorksheet);
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public abstract class Worksheet
        {
            public string Name { get; }

            protected Worksheet(string name)
            {
                Name = name;
            }

            public abstract void Render(IXLWorksheet worksheet);
        }
    }
}