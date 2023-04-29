using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Reflection;
using Observatory.Framework.Files.ParameterTypes;
using Syncfusion.Maui.DataGrid;

namespace ObservatoryUI.ViewModels
{
    public class PluginObservableList : IList<object>, INotifyCollectionChanged
    {
        readonly List<object> _innerList = new List<object>();

        int _updateCount;
        readonly List<object> _adds = new List<object>();
        readonly List<object> _removes = new List<object>();

        public object this[int index] 
        {
            get {
                return _innerList[index];
            }
            set {
                PopulateColumnInfo(value);
                _innerList[index] = value;
            }
        }

        public int Count => _innerList.Count;

        public bool IsReadOnly => false;

        public List<PluginColumnInfo> Columns { get; private set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler ColumnsChanged;


        public int BeginUpdate()
        {
            return Interlocked.Increment(ref _updateCount);
        }

        public int EndUpdate(bool performReset = false)
        {
            int count = Interlocked.Decrement(ref _updateCount);
            if (count < 1)
            {
                Interlocked.Exchange(ref _updateCount, 0);

                if(performReset || _adds.Count + _removes.Count > 1000)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                else if (_adds.Count > 0 && _removes.Count > 0)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _adds, _removes));
                else if(_adds.Count > 0)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _adds));
                else if(_removes.Count > 0)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _removes));

                _adds.Clear();
                _removes.Clear();
            }
            return count;
        }

        public void Add(object item)
        {
            PopulateColumnInfo(item);
            _innerList.Add(item);
            if (_updateCount == 0)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            else
            {
                _adds.Add(item);
                _removes.Remove(item);
            }
        }

        public void AddRange(IEnumerable<object> collection)
        {
            object[] items = collection.ToArray();
            if (Columns == null && items.Length > 0)
                PopulateColumnInfo(items.First());

            _innerList.AddRange(items);
            if(_updateCount == 0)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
            else
            {
                _adds.AddRange(items);
                _removes.RemoveAll(i => items.Contains(i));
            }
        }

        public void Clear()
        {
            if(_updateCount == 0)
            {
                var removed = _innerList.ToArray();
                _innerList.Clear();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
            }
            else
            {
                _removes.AddRange(_innerList);
                _adds.Clear();
                _innerList.Clear();
            }
        }

        public bool Contains(object item)
        {
            PopulateColumnInfo(item);
            return _innerList.Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public int IndexOf(object item)
        {
            PopulateColumnInfo(item);
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, object item)
        {
            PopulateColumnInfo(item);
            _innerList.Insert(index, item);
            if (_updateCount == 0)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            else
            {
                _adds.Add(item);
                _removes.Remove(item);
            }
        }

        public bool Remove(object item)
        {
            PopulateColumnInfo(item);
            if (_innerList.Remove(item))
            {
                if (_updateCount == 0)
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                else
                {
                    _removes.Add(item);
                    _adds.Remove(item);
                }
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            var item = _innerList[index];
            Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        private void PopulateColumnInfo(object item)
        {
            if(Columns == null && item != null)
            {
                Type type = item.GetType();
                List<PluginColumnInfo> columns = new List<PluginColumnInfo>();

                foreach (var property in type.GetProperties())
                {
                    PluginColumnInfo columnInfo = new PluginColumnInfo();
                    columnInfo.PropertyInfo = property;
                    columnInfo.HeaderText = property.Name;

                    var display = property.GetCustomAttribute<DisplayAttribute>();
                    if (display != null)
                    {
                        columnInfo.DisplayField = display.AutoGenerateField;
                        columnInfo.DisplayFilter = display.AutoGenerateFilter;
                        if(!String.IsNullOrEmpty(display.Name))
                            columnInfo.HeaderText = display.Name;
                    }

                    var format = property.GetCustomAttribute<DisplayFormatAttribute>();
                    if(format != null)
                    {
                        columnInfo.DisplayFormat = format.DataFormatString;
                        columnInfo.DisplayNullValue = format.NullDisplayText;
                    }

                    var propType = property.PropertyType;
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propType = propType.GetGenericArguments().First();

                    if (propType == typeof(int) ||
                        propType == typeof(float) ||
                        propType == typeof(double) ||
                        propType == typeof(decimal))
                    {
                        columnInfo.GridColumnType = typeof(DataGridNumericColumn);
                    }
                    else if (propType == typeof(DateTime) ||
                        propType == typeof(DateTimeOffset))
                    {
                        columnInfo.GridColumnType = typeof(DataGridDateColumn);
                    }
                    else if (propType == typeof(bool))
                    {
                        columnInfo.GridColumnType = typeof(DataGridCheckBoxColumn);
                    }
                    else if (propType == typeof(Bitmap) || propType == typeof(Icon))
                    {
                        columnInfo.GridColumnType = typeof(DataGridImageColumn);
                    }
                    else if (propType == typeof(string))
                    {
                        columnInfo.GridColumnType = typeof(DataGridTextColumn);
                    }
                }

                this.Columns = columns;
                ColumnsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

    }
}
