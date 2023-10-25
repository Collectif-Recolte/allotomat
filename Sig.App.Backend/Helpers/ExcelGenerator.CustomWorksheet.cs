using System;
using ClosedXML.Excel;

namespace Sig.App.Backend.Helpers
{
    public partial class ExcelGenerator
    {
        public void AddCustomWorksheet(string name, Action<IXLWorksheet> renderWorksheet)
        {
            worksheets.Add(new CustomWorksheet(name, renderWorksheet));
        }
        
        private class CustomWorksheet : Worksheet
        {
            private readonly Action<IXLWorksheet> renderWorksheet;

            public CustomWorksheet(string name, Action<IXLWorksheet> renderWorksheet) : base(name)
            {
                this.renderWorksheet = renderWorksheet;
            }

            public override void Render(IXLWorksheet worksheet) => renderWorksheet(worksheet);
        }
    }
}