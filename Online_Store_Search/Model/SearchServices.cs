using Microsoft.EntityFrameworkCore;
using Online_Store_Search.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Online_Store_Search.Model
{
    public class SearchService
    {
        private readonly OnlineStoreContext _context;
        private Dictionary<string, Tuple<int, int>[]> priceRanges;

        public SearchService(OnlineStoreContext context)
        {
             _context = context;
        }


        public DataTable ToDataTable(List<IProduct> items, string tableName)
        {
            if (items.Count == 0)
                return new DataTable();

            var dataTable = new DataTable(tableName);

            // Get first item to determine actual type (Car, Laptop, TV)
            var firstItem = items[0];

            // Use properties of actual class, not interface
            var properties = firstItem.GetType().GetProperties();
            var columnNames = new HashSet<string>();

            foreach (var property in properties)
            {
                var columnName = property.Name;
                var columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                // Check for duplicate column names
                if (columnNames.Contains(columnName))
                {
                    // Handle duplicate column names
                    int suffix = 1;
                    var newColumnName = columnName;

                    while (columnNames.Contains(newColumnName))
                    {
                        newColumnName = $"{columnName}_{suffix++}";
                    }

                    columnName = newColumnName;
                }

                columnNames.Add(columnName);
                dataTable.Columns.Add(columnName, columnType);
            }

            foreach (var item in items)
            {
                var newRow = dataTable.NewRow();

                foreach (var property in item.GetType().GetProperties())
                {
                    var columnName = property.Name;
                    var columnValue = property.GetValue(item);

                    if (columnValue != null)
                    {
                        if (columnValue is IProduct product)
                        {
                            // Handle the case where the property is an instance of a class implementing IProduct
                            foreach (var productProperty in product.GetType().GetProperties())
                            {
                                var productPropertyName = productProperty.Name;
                                var productPropertyValue = productProperty.GetValue(product);

                                newRow[productPropertyName] = productPropertyValue ?? DBNull.Value;
                            }
                        }
                        else
                        {
                            newRow[columnName] = columnValue ?? DBNull.Value;
                        }
                    }
                }

                dataTable.Rows.Add(newRow);
            }

            return dataTable;
        }

        public DataTable FilterDataSet(string selectedTable, string selectedColumn1, string value1, string selectedColumn2, string value2)
        {
            // Using reflection to get the property (DbSet) by name
            var dbSetProperty = _context.GetType().GetProperty(selectedTable);
            if (dbSetProperty == null)
            {
                throw new InvalidOperationException($"Unsupported table: {selectedTable}");
            }

            // Get the DbSet and queryable
            var dbSet = (IQueryable<IProduct>)dbSetProperty.GetValue(_context);

            // Prepare the filter
            List<string> filterParts = new List<string>();
            List<object> filterValues = new List<object>();

            ParseValue(selectedColumn1, value1, filterParts, filterValues);
            ParseValue(selectedColumn2, value2, filterParts, filterValues);

            // Apply the filter if it's not empty
            if (filterParts.Count > 0)
            {
                string filterExpression = string.Join(" AND ", filterParts);
                dbSet = dbSet.Where(filterExpression, filterValues.ToArray());
            }

            // Execute the query
            var filteredCollection = dbSet.ToList();

            // Convert the collection to a DataTable
            var filteredDataTable = ToDataTable(filteredCollection, selectedTable);

            // Return the DataTable
            return filteredDataTable;
        }

        private void ParseValue(string selectedColumn, string value, List<string> filterParts, List<object> filterValues)
        {
            if (!string.IsNullOrEmpty(selectedColumn) && value != null)
            {
                var intervalMatch = Regex.Match(value, @"([<>]=?)(\d+)&&([<>]=?)(\d+)");

                if (intervalMatch.Success)
                {
                    var operatorPart1 = intervalMatch.Groups[1].Value;
                    var valuePart1 = int.Parse(intervalMatch.Groups[2].Value);
                    filterParts.Add($"{selectedColumn} {operatorPart1} @{filterValues.Count}");
                    filterValues.Add(valuePart1);

                    var operatorPart2 = intervalMatch.Groups[3].Value;
                    var valuePart2 = int.Parse(intervalMatch.Groups[4].Value);
                    filterParts.Add($"{selectedColumn} {operatorPart2} @{filterValues.Count}");
                    filterValues.Add(valuePart2);
                }
                else
                {
                    var match = Regex.Match(value, @"([<>]=?)(\d+)");
                    if (match.Success)
                    {
                        var operatorPart = match.Groups[1].Value;
                        var valuePart = int.Parse(match.Groups[2].Value);
                        filterParts.Add($"{selectedColumn} {operatorPart} @{filterValues.Count}");
                        filterValues.Add(valuePart);
                    }
                    else if (int.TryParse(value, out int parsedValue))
                    {
                        filterParts.Add($"{selectedColumn} = @{filterValues.Count}");
                        filterValues.Add(parsedValue);
                    }
                    else
                    {
                        filterParts.Add($"{selectedColumn} = @{filterValues.Count}");
                        filterValues.Add(value);
                    }
                }
            }
        }

    }


}

