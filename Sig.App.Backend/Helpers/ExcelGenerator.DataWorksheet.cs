using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace Sig.App.Backend.Helpers
{
    public partial class ExcelGenerator
    {
        public DataWorksheet<T> AddDataWorksheet<T>(
            string name,
            IEnumerable<T> data,
            Action<IXLRow> applyHeadingRowStyle = null
        )
        {
            var worksheet = new DataWorksheet<T>(name, data, applyHeadingRowStyle);
            worksheets.Add(worksheet);
            return worksheet;
        }

        public class DataWorksheet<T> : Worksheet
        {
            private readonly List<Action<IXLCell>> headingRenderers = new();
            private readonly List<Action<T, IXLCell>> cellRenderers = new();
            private readonly List<Action<IXLRow>> footerRenderers = new();

            private readonly IEnumerable<T> data;
            private readonly Action<IXLRow> applyHeadingRowStyle;

            public DataWorksheet(
                string name,
                IEnumerable<T> data,
                Action<IXLRow> applyHeadingRowStyle = null
            ) : base(name)
            {
                this.data = data;
                this.applyHeadingRowStyle = applyHeadingRowStyle ?? (x =>
                {
                    x.Style.Fill.BackgroundColor = XLColor.Black;
                    x.Style.Font.FontColor = XLColor.White;
                    x.Style.Font.Bold = true;
                });
            }

            public DataWorksheet<T> Column(Action<IXLCell> renderHeading, Action<T, IXLCell> renderCell)
            {
                headingRenderers.Add(renderHeading);
                cellRenderers.Add(renderCell);

                return this;
            }

            public DataWorksheet<T> Column(string heading, Action<T, IXLCell> renderCell)
            {
                return Column(
                    x => x.Value = heading,
                    renderCell
                );
            }

            public DataWorksheet<T> Column(Action<IXLCell> renderHeading, Func<T, object> getValue)
            {
                return Column(
                    renderHeading,
                    (item, cell) => cell.Value = getValue(item)
                );
            }

            public DataWorksheet<T> Column(string heading, Func<T, object> getValue)
            {
                return Column(
                    x => x.Value = heading,
                    (item, cell) => cell.Value = getValue(item)
                );
            }

            public DataWorksheet<T> AddFooterRow(Action<IXLRow> renderRow)
            {
                footerRenderers.Add(renderRow);
                return this;
            }

            public override void Render(IXLWorksheet xlWorksheet)
            {
                WriteHeader(xlWorksheet);

                var currentRow = 2;

                foreach (var dataItem in data)
                    WriteRow(xlWorksheet.Row(currentRow++), dataItem);
                foreach (var footerRenderer in footerRenderers)
                    footerRenderer(xlWorksheet.Row(currentRow++));
            }

            private void WriteHeader(IXLWorksheet worksheet)
            {
                var row = worksheet.Row(1);

                applyHeadingRowStyle(row);

                var currentCell = 1;
                foreach (var headingRenderer in headingRenderers)
                    headingRenderer(row.Cell(currentCell++));
            }

            private void WriteRow(IXLRow row, T dataItem)
            {
                var currentCell = 1;
                foreach (var cellRenderer in cellRenderers)
                    cellRenderer(dataItem, row.Cell(currentCell++));
            }
        }
    }
}