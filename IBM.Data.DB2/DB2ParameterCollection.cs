
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Data;
using System.Collections;
using System.Globalization;
using System.Data.Common;
using System.Collections.Generic;

namespace IBM.Data.DB2
{

    public class DB2ParameterCollection : DbParameterCollection
    {
        private IntPtr hwndStmt = IntPtr.Zero;
        private IList<DB2Parameter> parameters = new List<DB2Parameter>();

        internal IntPtr HwndStmt
        {
            set
            {
                hwndStmt = value;
            }
        }

        public override int Count
        {
            get { return parameters.Count; }
        }

        //true if the IList has a fixed size; otherwise, false. In the default implementation of List<T>, this property always returns false.
        public override bool IsFixedSize
        {
            get { return false; }
        }

        public override bool IsReadOnly
        {
            get { return parameters.IsReadOnly; }
        }

        //true if access to the ICollection is synchronized(thread safe); otherwise, false. In the default implementation of List<T>, this property always returns false.
        public override bool IsSynchronized
        {
            get { return false; }
        }

        //An object that can be used to synchronize access to the ICollection. In the default implementation of List<T>, this property always returns the current instance.
        public override object SyncRoot
        {
            get { return this; }
        }

        public new DB2Parameter this[int index]
        {
            get
            {
                return (DB2Parameter)parameters[index];
            }
            set
            {
                this[IndexOf(index)] = (DB2Parameter)value;
            }
        }

        public DB2Parameter this[string index]
        {
            get
            {
                return (DB2Parameter)parameters[IndexOf(index)];
            }
            set
            {
                this[IndexOf(index)] = (DB2Parameter)value;
            }
        }

        public override bool Contains(string paramName)
        {
            return (-1 != IndexOf(paramName));
        }

        public override int IndexOf(string paramName)
        {
            int index = 0;
            for (index = 0; index < Count; index++)
            {
                if (0 == CultureAware(((DB2Parameter)this[index]).ParameterName, paramName))
                {
                    return index;
                }
            }
            return -1;
        }

        public override void RemoveAt(string paramName)
        {
            RemoveAt(IndexOf(paramName));
        }

        public override int Add(object obj)
        {
            DB2Parameter value = (DB2Parameter)obj;
            if (value.ParameterName == null)
                throw new ArgumentException("parameter must be named");
            if (IndexOf(value.ParameterName) >= 0)
                throw new ArgumentException("parameter name is already in collection");

            parameters.Add(value);

            return Count - 1;
        }

        public DB2Parameter Add(DB2Parameter value)
        {
            if (value.ParameterName == null)
                throw new ArgumentException("parameter must be named");
            if (IndexOf(value.ParameterName) >= 0)
                throw new ArgumentException("parameter name is already in collection");
            parameters.Add(value);
            return value;
        }

        public DB2Parameter Add(string paramName, DB2Type type)
        {
            return Add(new DB2Parameter(paramName, type));
        }

        public DB2Parameter Add(string paramName, object value)
        {
            return Add(new DB2Parameter(paramName, value));
        }

        public DB2Parameter Add(string paramName, DB2Type dbType, int size)
        {
            return Add(new DB2Parameter(paramName, dbType, size));
        }

        public DB2Parameter Add(string paramName, DB2Type dbType, int size, string sourceColumn)
        {
            return Add(new DB2Parameter(paramName, dbType, size, sourceColumn));
        }

        private int CultureAware(string strA, string strB)
        {
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
        }

        internal void GetOutValues()
        {
            foreach (DB2Parameter param in this)
            {
                if (ParameterDirection.Output == param.Direction || ParameterDirection.InputOutput == param.Direction)
                {
                    param.GetOutValue();
                    //Console.WriteLine(param.ParameterName);
                }
            }
        }

        public override void AddRange(Array values)
        {
            foreach (DB2Parameter db2Parameter in values)
                parameters.Add(db2Parameter);
        }

        public override bool Contains(object value)
        {
            return parameters.IndexOf((DB2Parameter)value) != -1;
        }

        public override void CopyTo(Array array, int index)
        {
            parameters.CopyTo((DB2Parameter[])array, index);
        }

        public override void Clear()
        {
            parameters.Clear();
        }

        public override IEnumerator GetEnumerator()
        {
            return (IEnumerator)parameters.GetEnumerator();
        }

        protected override DbParameter GetParameter(int index)
        {
            return (DbParameter)parameters[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            int index = IndexOf(parameterName);
            return (DbParameter)parameters[index];
        }

        public override int IndexOf(object value)
        {
            return parameters.IndexOf((DB2Parameter)value);
        }

        public override void Insert(int index, object value)
        {
            parameters[index] = (DB2Parameter)value;
        }

        public override void Remove(object value)
        {
            parameters.Remove((DB2Parameter)value);
        }

        public override void RemoveAt(int index)
        {
            parameters.RemoveAt(index);
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            parameters[index] = (DB2Parameter)value;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            parameters[IndexOf(parameterName)] = (DB2Parameter)value;
        }
    }
}

