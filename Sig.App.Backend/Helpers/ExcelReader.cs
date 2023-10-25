using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sig.App.Backend.Helpers;

public class ExcelReader
{
    private readonly XLWorkbook workbook;
    private int worksheetNumber = 0;

    private ExcelReader(Stream excelStream)
    {
        workbook = new XLWorkbook(excelStream);
    }

    public static bool TryOpenStream(Stream stream, out ExcelReader reader, Action<Exception> onError = null)
    {
        try
        {
            reader = new ExcelReader(stream);
            return true;
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
                
            reader = null;
            return false;
        }
    }

    public WorksheetReader<T> Worksheet<T>(int countHeaderRows = 1) where T : new() => new(workbook.Worksheet(++worksheetNumber), countHeaderRows + 1);
}

public class WorksheetReader<T> where T : new()
{
    private readonly List<Action<IXLRow, T>> rowMappers = new();
    private readonly List<Action<IXLCell, T>> cellMappers = new();
        
    private readonly IXLWorksheet worksheet;
    private readonly int startRow;

    public WorksheetReader(IXLWorksheet worksheet, int startRow)
    {
        this.worksheet = worksheet;
        this.startRow = startRow;
    }

    public WorksheetReader<T> Column(Action<IXLCell, T> mapper)
    {
        cellMappers.Add(mapper);
        return this;
    }

    public WorksheetReader<T> RowNumber(Expression<Func<T, int>> func)
    {
        var property = func.GetPropertyAccess();
        rowMappers.Add((row, item) => property.SetValue(item, row.RowNumber()));
        return this;
    }

    public WorksheetReader<T> String(Expression<Func<T, string>> func)
    {
        var property = func.GetPropertyAccess();
        return Column((cell, item) => property.SetValue(item, cell.GetString()?.Trim()));
    }

    public IEnumerable<T> GetData()
    {
        var row = worksheet.Row(startRow);
            
        while (!row.IsEmpty())
        {
            var item = new T();

            foreach (var mapper in rowMappers)
            {
                mapper(row, item);
            }
            
            var cellNumber = 0;
            foreach (var mapper in cellMappers)
            {
                mapper(row.Cell(++cellNumber), item);
            }

            yield return item;
            row = row.RowBelow();
        }
    }
}