using Microsoft.EntityFrameworkCore;
using Online_Store_Search.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Store_Search.Model
{
    public static class SearchService
    {
        private readonly OnlineStoreContext _context;

        public PcStoreSearch(PCBuilderContext context)
        {
            _context = context;
        }
        public static void ExecuteSearchCommand()
        {
            // Generate filter expression
            var filterExpression = GetFilterExpression();

            // Set the CurrentDataSet based on the selected table and filters
            if (_collections.TryGetValue(_selectedTable, out var collection))
            {
                var filteredCollection = FilterCollection(collection.ToList(), filterExpression);
                var filteredDataTable = SearchService.ToDataTable(filteredCollection, SelectedTable);
                CurrentDataSet = filteredDataTable.DefaultView;

                collection.Clear();
                foreach (IProduct product in filteredCollection)
                {
                    collection.Add(product);
                }
            }
        }

        public static DataTable ToDataTable<T>(List<T> items, string tableName)
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

        public static void FilterDataSet(string _selectedTable, string _selectedColumn, string _selectedValue)
        {
            // Using reflection to get the property (DbSet) by name
            var dbSetProperty = _context.GetType().GetProperty(_selectedTable);
            if (dbSetProperty == null)
            {
                throw new InvalidOperationException($"Unsupported table: {_selectedTable}");
            }

            var dbSet = (DbSet<IProduct>)dbSetProperty.GetValue(_context);

            // Create an IQueryable that will hold the query
            IQueryable<IProduct> query = dbSet;

            // Apply filters
            if (!string.IsNullOrEmpty(_selectedColumn) && !string.IsNullOrEmpty(_selectedValue))
            {
                query = query.Where($"{_selectedColumn} = @0", _selectedValue);
            }

            foreach (var filter in Filters)
            {
                if (!string.IsNullOrEmpty(filter.SelectedColumn) && !string.IsNullOrEmpty(filter.SelectedValue))
                {
                    query = query.Where($"{filter.SelectedColumn} = @0", filter.SelectedValue);
                }
            }

            // Execute the query and update the current data set
            var filteredCollection = query.ToList();
            var filteredDataTable = ToDataTable(filteredCollection, _selectedTable);
            CurrentDataSet = filteredDataTable.DefaultView;
        }
    }
}