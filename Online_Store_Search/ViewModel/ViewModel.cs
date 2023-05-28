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
        private readonly SearchService _searchService;
        public ObservableCollection<IProduct> Cars { get; set; }
        public ObservableCollection<IProduct> Laptops { get; set; }
        public ObservableCollection<IProduct> TVs { get; set; }
        public ICommand AddFilterCommand { get; private set; }
        public ICommand RemoveFiltersCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ToggleTablesCommand { get; }

        private Dictionary<string, ObservableCollection<IProduct>> _collections;
        public List<Filter> Filters { get; } = new List<Filter>();
        private OnlineStoreContext _context;
        private Dictionary<string, DataTable> _dataSets;




        public MainViewModel()
        {
            _searchService = new SearchService(new OnlineStoreContext());
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFiltersCommand = new RelayCommand(RemoveFilters);
            SearchCommand = new RelayCommand(_ => Search(), _ => true);
            _context = new OnlineStoreContext();
            Cars = new ObservableCollection<IProduct>(_context.Cars.ToList());
            Laptops = new ObservableCollection<IProduct>(_context.Laptops.ToList());
            TVs = new ObservableCollection<IProduct>(_context.TVs.ToList());
            ToggleTablesCommand = new RelayCommand(ToggleTablesVisibility);


            _collections = new Dictionary<string, ObservableCollection<IProduct>>
            {
                { nameof(_context.Cars), Cars },
                { nameof(_context.Laptops), Laptops },
                { nameof(_context.TVs), TVs }
            };
            _dataSets = new Dictionary<string, DataTable>()
            {
                { nameof(_context.Cars), _searchService.ToDataTable(_context.Cars.ToList<IProduct>(), nameof(_context.Cars)) },
                { nameof(_context.Laptops), _searchService.ToDataTable(_context.Laptops.ToList<IProduct>(), nameof(_context.Laptops)) },
                { nameof(_context.TVs), _searchService.ToDataTable(_context.TVs.ToList<IProduct>(), nameof(_context.TVs)) }
            };
            Tables = new ObservableCollection<string>(_dataSets.Keys);
            Columns = new ObservableCollection<string>();
            Values = new ObservableCollection<string>();
            Values2 = new ObservableCollection<string>();
            Columns2 = new ObservableCollection<string>();
            Filters.Add(new Filter(_dataSets, _selectedTable));
        }

        public void ExecuteSearchCommand()
        {
            // Generate filter expression
            var filterExpression = GetFilterExpression();

            // Set the CurrentDataSet based on the selected table and filters
            if (_collections.TryGetValue(_selectedTable, out var collection))
            {
                var filteredCollection = FilterCollection(collection.ToList(), filterExpression);
                var filteredDataTable = _searchService.ToDataTable(filteredCollection, SelectedTable);
                CurrentDataSet = filteredDataTable.DefaultView;

                collection.Clear();
                foreach (IProduct product in filteredCollection)
                {
                    collection.Add(product);
                }
            }
        }


        public ObservableCollection<string> Tables { get; set; }
        public ObservableCollection<string> Columns { get; set; }
        public ObservableCollection<string> Values { get; set; }

        public ObservableCollection<string> Columns2 { get; set; }
        public ObservableCollection<string> Values2 { get; set; }

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
                    Columns2.Clear();
                    foreach (var column in dataSet.Columns.Cast<DataColumn>().Select(c => c.ColumnName))
                    {
                        Columns.Add(column);
                        Columns2.Add(column);
                    }
                }
                CurrentDataSet = _searchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue, SelectedColumn2, SelectedValue2).DefaultView;
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
                    int temp;
                    Dictionary<string, Tuple<int, int>[]> priceRanges;
                    try
                    {
                        foreach (var value1 in dataSet.AsEnumerable().Select(row => row[_selectedColumn]?.ToString()).Distinct())
                        {
                            if (int.TryParse(value1, out temp))
                            {
                                // Intervals for Size in inches
                                Values.Add("<32");
                                Values.Add(">=33&&<55");
                                Values.Add(">=56&&<65");
                                Values.Add(">=65&&<100");
                                // Horse Power Ranges
                                Values.Add(">=100&&<150");
                                Values.Add(">=150&&<200");
                                Values.Add(">=200&&<300");
                                Values.Add("<=300&&500");
                                // Price Ranges
                                Values.Add(">=500&&<1000");
                                Values.Add(">=1000&&<5000");
                                Values.Add(">=5000&&<10000");
                                Values.Add(">=10000&&<50000");
                                Values.Add(">=50000");
                                break;
                            }
                            else Values.Add(value1);
                        }
                    }
                    catch (ArgumentNullException ex) { }
                }
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
            }
        }

        private string _selectedColumn2;
        public string SelectedColumn2
        {
            get => _selectedColumn2;
            set
            {
                _selectedColumn2 = value;
                OnPropertyChanged();
                if (_dataSets.TryGetValue(_selectedTable, out var dataSet))
                {
                    Values2.Clear();
                    foreach (var value1 in dataSet.AsEnumerable().Select(row => row[_selectedColumn2]?.ToString()).Distinct())
                    {
                        Values2.Add(value1);
                    }
                }
                // Set the current filter when the column is changed
            }
        }



        private string _selectedValue2;
        public string SelectedValue2
        {
            get => _selectedValue2;
            set
            {
                _selectedValue2 = value;
                OnPropertyChanged();
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
            Filter hiddenFilter = Filters.FirstOrDefault(f => !f.Visible);
            if (hiddenFilter != null)
            {
                hiddenFilter.Visible = true;
                hiddenFilter._selectedTable = _selectedTable;
                hiddenFilter.PropertyChanged += NewFilter_PropertyChanged;
            }
            else
            {
                var newFilter = new Filter(_dataSets, _selectedTable) { Visible = true };
                Filters.Add(newFilter);
            }
        }


        public void RemoveFilters(object parameter)
        {
            // Instead of clearing all filters, make them invisible
            foreach (Filter filter in Filters)
            {
                filter.Visible = false;
                filter.Values2.Clear();
                // Columns2.Clear();
            }
        }

        public void Search()
        {
            var parts = new DataTable();

            if (Filters.Count == 0)
            {
                parts = _searchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue, null, null);
            }
            else
            {
                foreach (var filter in Filters)
                {
                    if (Filters.IndexOf(filter) == 0)
                    {
                        parts = _searchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue, filter.SelectedColumn2, filter.SelectedValue2);
                    }
                    else
                    {
                        var filterableParts = parts.AsEnumerable().AsQueryable();
                        parts = _searchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue, filter.SelectedColumn2, filter.SelectedValue2);
                    }
                }
            }

            CurrentDataSet = parts.DefaultView;
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

        public void ApplyFilters(string selectedTable, string selectedColumn, string selectedValue)
        {
            // Get filtered data from model layer
            var filteredCollection = _searchService.FilterDataSet(_selectedTable, _selectedColumn, _selectedValue, SelectedColumn2, SelectedValue2);

            // var filteredDataTable = _searchService.ToDataTable(filteredCollection, selectedTable);

            // Apply filters from Filters property and update CurrentDataSet
            string filterExpression = "";
            foreach (var filter in Filters)
            {
                if (!string.IsNullOrEmpty(filter.SelectedColumn2) && !string.IsNullOrEmpty(filter.SelectedValue2))
                {
                    filterExpression += $" AND {filter.SelectedColumn2} = '{filter.SelectedValue2}'";
                }
            }

            CurrentDataSet = new DataView(filteredCollection, filterExpression, "", DataViewRowState.CurrentRows);
        }

        private void NewFilter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Filter.Values2))
            {
                var filter = sender as Filter;
                Values2 = filter.Values2;
            }
        }

        private bool _areExtraTablesVisible = false;
        public bool AreExtraTablesVisible
        {
            get => _areExtraTablesVisible;
            set
            {
                _areExtraTablesVisible = value;
                OnPropertyChanged(nameof(AreExtraTablesVisible));
            }
        }

        private void ToggleTablesVisibility(object parameter)
        {
            AreExtraTablesVisible = !AreExtraTablesVisible;
        }


        public class Filter : INotifyPropertyChanged
        {
            private Dictionary<string, DataTable> _dataSets;
            public string _selectedTable;
            private ObservableCollection<string> _values2;

            public Filter(Dictionary<string, DataTable> dataSets, string selectedTable)
            {
                _dataSets = dataSets;
                _selectedTable = selectedTable;
                _values2 = new ObservableCollection<string>();
            }

            public ObservableCollection<string> Values2
            {
                get => _values2;
                set
                {
                    if (_values2 != value)
                    {
                        _values2 = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string FilterColumn { get; set; }
            public string FilterValue { get; set; }

            private string _selectedColumn2;
            public string SelectedColumn2
            {
                get => _selectedColumn2;
                set
                {
                    _selectedColumn2 = value;
                    if (_dataSets != null)
                    {
                        if (_dataSets.TryGetValue(_selectedTable, out var dataSet))
                        {
                            Values2.Clear();
                            foreach (var value1 in dataSet.AsEnumerable().Select(row => row[_selectedColumn2]?.ToString()).Distinct())
                            {
                                Values2.Add(value1);
                            }
                        }
                    }
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

            private string _selectedValue2;
            public string SelectedValue2
            {
                get => _selectedValue2;
                set
                {
                    _selectedValue2 = value;
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
}

