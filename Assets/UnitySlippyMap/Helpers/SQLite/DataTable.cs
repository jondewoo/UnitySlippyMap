using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class DataRow : Dictionary<string, object>
{
    public new object this[string column]
    {
        get
        {
            if (ContainsKey(column))
            {
                return base[column];
            }
            
            return null;
        }
        set
        {
            if (ContainsKey(column))
            {
                base[column] = value;
            }
            else
            {
                Add(column, value);
            }
        }
    }
}

public class DataTable
{
    public DataTable()
    {
        Columns = new List<string>();
        Rows = new List<DataRow>();
    }
    
    public List<string> Columns { get; set; }
    public List<DataRow> Rows { get; set; }
    
    public DataRow this[int row]
    {
        get
        {
            return Rows[row];
        }
    }
    
    public void AddRow(object[] values)
    {
        if (values.Length != Columns.Count)
        {
            throw new IndexOutOfRangeException("The number of values in the row must match the number of column");
        }
        
        var row = new DataRow();
        for (int i = 0; i < values.Length; i++)
        {
            row[Columns[i]] = values[i];
        }
        
        Rows.Add(row);
    }
}

