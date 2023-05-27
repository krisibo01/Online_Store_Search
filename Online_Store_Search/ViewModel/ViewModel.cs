using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Linq;
using Online_Store_Search.Model;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Online_Store_Search.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<IProduct> Cars { get; set; }
        public ObservableCollection<IProduct> Laptops { get; set; }
        public ObservableCollection<IProduct> TVs { get; set; }
        public ICommand AddFilterCommand { get; private set; }
        public ICommand RemoveFiltersCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        private Dictionary<string, ObservableCollection<IProduct>> _collections;
        public List<Filter> Filters { get; } = new List<Filter>();
        private OnlineStoreContext _context;
        private Dictionary<string, DataTable> _dataSets;



        public MainViewModel()
        {
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFiltersCommand = new RelayCommand(RemoveFilters);
            SearchCommand = new RelayCommand(o => SearchService.ExecuteSearchCommand());
            _context = new OnlineStoreContext();
            Cars = new ObservableCollection<IProduct>(_context.Cars.ToList());
            Laptops = new ObservableCollection<IProduct>(_context.Laptops.ToList());
            TVs = new ObservableCollection<IProduct>(_context.TVs.ToList());
            Columns = new ObservableCollection<string>();
            Values = new ObservableCollection<string>();
            _collections = new Dictionary<string, ObservableCollection<IProduct>>
{
    { nameof(_context.Cars), Cars },
    { nameof(_context.Laptops), Laptops },
    { nameof(_context.TVs), TVs }
};
            Filters.Add(new Filter());

        }




        public ObservableCollection<string> Tables { get; set; }
        public ObservableCollection<string> Columns { get; set; }
        public ObservableCollection<string> Values { get; set; }

        private string _selectedTable;
        public string SelectedTable
        {
            get => _selectedTable;
            set
            {
                _selectedTable = value;
                OnPropertyChanged();
                if (_dataSets.TryGetValue(_selectedTable, out var dataSet) && dataSet.Rows.Count > 0)
                {
                    Columns.Clear();
                    foreach (var column in dataSet.Columns.Cast<DataColumn>().Select(c => c.ColumnName))
                    {
                        Columns.Add(column);
                    }
                }
                SearchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue);
            }
        }

        private string _selectedColumn;
        public string SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                _selectedColumn = value;
                OnPropertyChanged();
                if (_dataSets.TryGetValue(_selectedTable, out var dataSet))
                {
                    Values.Clear();
                    foreach (var value1 in dataSet.AsEnumerable().Select(row => row[_selectedColumn]?.ToString()).Distinct())
                    {
                        Values.Add(value1);
                    }
                }
                SearchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue);
            }
        }


        private string _selectedValue;
        public string SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                OnPropertyChanged();
                SearchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue);
            }
        }

        private DataView _currentDataSet;
        public DataView CurrentDataSet
        {
            get => _currentDataSet;
            set
            {
                if (_currentDataSet != value)
                {
                    _currentDataSet = value;
                    OnPropertyChanged();
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddFilter(object parameter)
        {
            // If there is a hidden filter, make it visible
            Filter hiddenFilter = Filters.FirstOrDefault(f => !f.Visible);
            if (hiddenFilter != null)
            {
                hiddenFilter.Visible = true;
            }
            else
            {
                // Otherwise, add a new filter
                Filters.Add(new Filter { Visible = true });
            }
        }

        public void RemoveFilters(object parameter)
        {
            // Instead of clearing all filters, make them invisible
            foreach (Filter filter in Filters)
            {
                filter.Visible = false;
            }
        }

        public void Search(object parameter)
        {
            SearchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue);
        }

        private List<IProduct> FilterCollection(List<IProduct> collection, string filterExpression)
        {
            // Assume filterExpression is in the format "Property1 = 'Value1' AND Property2 = 'Value2' AND ..."
            var filters = filterExpression.Split(new string[] { " AND " }, StringSplitOptions.None)
                                          .Select(s => new
                                          {
                                              Property = s.Split(new string[] { " = " }, StringSplitOptions.None)[0],
                                              Value = s.Split(new string[] { " = " }, StringSplitOptions.None)[1].Trim('\'')
                                          })
                                          .ToList();

            var filteredCollection = collection;

            foreach (var filter in filters)
            {
                filteredCollection = filteredCollection.Where(p =>
                {
                    var property = p.GetType().GetProperty(filter.Property);
                    if (property == null) return false;
                    var value = property.GetValue(p)?.ToString();
                    return value == filter.Value;
                }).ToList();
            }

            return filteredCollection;
        }

       

        private IProduct ConvertRowToProduct(DataRow row, string tableName)
        {
            // Get the namespace of your model classes
            string namespaceName = typeof(IProduct).Namespace;

            // Create a new instance of the appropriate class
            Type type = Type.GetType($"{namespaceName}.{tableName}");
            if (type == null || !typeof(IProduct).IsAssignableFrom(type))
            {
                throw new InvalidOperationException("Unsupported product type");
            }
            var product = (IProduct)Activator.CreateInstance(type);

            // Assuming the classes have properties that match the column names...
            foreach (DataColumn column in row.Table.Columns)
            {
                // Ignore RowError property
                if (column.ColumnName != "RowError")
                {
                    PropertyInfo property = type.GetProperty(column.ColumnName);
                    if (property != null)
                    {
                        object value = row[column.ColumnName];
                        if (value != DBNull.Value)
                        {
                            property.SetValue(product, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                }
            }
            return product;
        }





        private string GetFilterExpression()
        {
            var filters = new List<string>();
            foreach (var filter in Filters)
            {
                var filterValue = filter.FilterValue?.Replace("'", "''"); // Escape single quotes
                filters.Add($"{filter.FilterColumn} = '{filterValue}'");
            }
            return string.Join(" AND ", filters);
        }


    }


    public class Filter : INotifyPropertyChanged
    {
        public string FilterColumn { get; set; }
        public string FilterValue { get; set; }

        private string _selectedColumn;
        public string SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                _selectedColumn = value;
                OnPropertyChanged();
            }
        }
        // Add a property to control visibility
        private bool _visible;
        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                OnPropertyChanged();
            }
        }

        // Create constructor to set initial visibility to false
        public Filter()
        {
            Visible = false;
        }

        private string _selectedValue;
        public string SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}

