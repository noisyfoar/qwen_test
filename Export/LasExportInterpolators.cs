using NPFGEO.Collections;
using NPFGEO.Data;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NPFGEO.IO.LAS.Export
{
    public interface IInterpolator
    {
        void Interpolate(Curve orig, IList<double> x_new, IList<double?> y_new);
    }

    public class BufferedWriter<T> : IList<T>
    {
        StreamCollection<T> _source;
        T[] buffer;
        int _frame = -1;

        public BufferedWriter(StreamCollection<T> source, int bufferSize = 1024)
        {
            _source = source;
            buffer = new T[bufferSize];
        }

        public void Flush()
        {
            var startIndex = _frame * buffer.Length;
            int cnt = Math.Min(buffer.Length, _source.Count - startIndex);
            _source.Replace(startIndex, buffer, 0, cnt);
        }

        public void Reset()
        {
            _frame = -1;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public T this[int index]
        {
            set
            {
                int currFrame = index / buffer.Length;
                if (currFrame != _frame)
                {
                    if (_frame != -1)
                    {
                        Flush();
                    }

                    Array.Clear(buffer, 0, buffer.Length);
                    _frame = currFrame;
                }
                var innerIndex = index - currFrame * buffer.Length;
                buffer[innerIndex] = value;
            }
            get
            {
                int currFrame = index / buffer.Length;
                if (currFrame == _frame)
                {
                    var innerIndex = index - currFrame * buffer.Length;
                    return buffer[innerIndex];
                }
                else
                {
                    return _source[index];
                }
            }
        }

        public int Count => _source.Count;

        public bool IsReadOnly { get { return false; } }

        public void Add(T item)
        {
            Reset();
            _source.Add(item);
        }

        public void Clear()
        {
            Reset();
            _source.Clear();
        }

        public bool Contains(T item)
        {
            return _source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _source.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _source.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Reset();
            _source.Insert(index, item);
        }

        public bool Remove(T item)
        {
            Reset();
            return _source.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Reset();
            _source.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _source.GetEnumerator();
        }
    }

    public class NonInterpolator : IInterpolator
    {
        public void Interpolate(Curve orig, IList<double> x_new, IList<double?> y_new)
        {
            int index = 0;

            var x_new_count = x_new.Count;
            var x_orig_count_minus_one = orig.DepthMatrix.Rows - 1;

            for (int i = 0; i < x_new_count; i++)
            {
                while (true)
                {
                    if (index == x_orig_count_minus_one || orig.DepthMatrix[0, index] >= x_new[i])
                    {
                        break;
                    }

                    index++;
                }
                var d = orig.DepthMatrix[0, index];
                if (d >= x_new[i] && (i == x_new_count - 1 || d < x_new[i + 1]))
                {
                    y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, index]);
                }
                else
                {
                    y_new[i] = null;
                }
            }
        }
    }

    public class LinearInterpolator : IInterpolator
    {
        const bool UseNullValue = true;

        public void Interpolate(Curve orig, IList<double> x_new, IList<double?> y_new)
        {
            if (orig == null || x_new == null || y_new == null)
            {
                throw new NullReferenceException();
            }

            var orig_count = orig.DepthMatrix.Rows;
            var new_count = x_new.Count;

            if (new_count != y_new.Count)
            {
                throw new ArgumentException();
            }

            var orig_count_minus_one = orig_count - 1;

            double x_new_cur = default(double);

            double x_orig_next = default(double);
            double x_orig_cur = default(double);

            int index = 0;
            if (orig_count > 1)
            {
                x_orig_next = orig.DepthMatrix[0, 1];
                x_orig_cur = orig.DepthMatrix[0, 0];
            }

            for (int i = 0; i < new_count; i++)
            {
                x_new_cur = x_new[i];
                if (index > orig_count_minus_one)
                {
                    break;
                }

                while (true)
                {
                    if (index == orig_count_minus_one || x_orig_next >= x_new_cur)
                    {
                        break;
                    }

                    index++;
                    x_orig_cur = x_orig_next;
                    if (index == orig_count_minus_one)
                    {
                        break;
                    }

                    x_orig_next = orig.DepthMatrix[0, index + 1];
                }

                if (x_new_cur < x_orig_cur)
                {
                    if (UseNullValue)
                    {
                        y_new[i] = null;
                    }
                    else
                    {
                        y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, index]);
                    }
                }
                else if (index == orig_count_minus_one)
                {
                    if (UseNullValue)
                    {
                        y_new[i] = null;
                    }
                    else
                    {
                        y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, orig_count_minus_one]);
                    }
                }
                else
                {
                    y_new[i] = GetY(x_orig_cur, System.Convert.ToDouble(orig.DataMatrix[0, index]), x_orig_next, System.Convert.ToDouble(orig.DataMatrix[0, index + 1]), x_new_cur);
                }
            }
        }

        static double GetY(double x0, double y0, double x1, double y1, double x)
        {
            return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
        }
    }

    public class NextNeighborInterpolator : IInterpolator
    {
        const bool UseNullValue = true;

        public void Interpolate(Curve orig, IList<double> x_new, IList<double?> y_new)
        {
            int index = 0;

            var x_new_count = x_new.Count;
            var x_orig_count_minus_one = orig.DepthMatrix.Rows - 1;

            for (int i = 0; i < x_new_count; i++)
            {
                while (true)
                {
                    if (index == x_orig_count_minus_one || orig.DepthMatrix[0, index + 1] >= x_new[i])
                    {
                        break;
                    }

                    index++;
                }
                if (x_new[i] < orig.DepthMatrix[0, index])
                {
                    if (UseNullValue)
                    {
                        y_new[i] = null;
                    }
                    else
                    {
                        y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, index]);
                    }
                }
                else if (index == x_orig_count_minus_one)
                {
                    if (UseNullValue)
                    {
                        y_new[i] = null;
                    }
                    else
                    {
                        y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, x_orig_count_minus_one]);
                    }
                }
                else if ((x_new[i] - orig.DepthMatrix[0, index]) < (orig.DepthMatrix[0, index + 1] - x_new[i]))
                {
                    y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, index]);
                }
                else
                {
                    y_new[i] = System.Convert.ToDouble(orig.DataMatrix[0, index + 1]);
                }
            }
        }
    }
}
