using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace GameFramework.UI.Utilities
{
    /// <summary>
    /// Custom UI element that renders a simple line graph for performance metrics
    /// 
    /// Design:
    /// - Uses Unity's MeshGenerationContext for efficient rendering
    /// - Circular buffer for memory-efficient data storage
    /// - Configurable colors, ranges, and smoothing
    /// - Auto-scaling based on data range
    /// 
    /// Pros:
    /// - Efficient rendering using Unity's native mesh generation
    /// - Memory efficient with fixed-size circular buffer
    /// - Smooth visual updates with configurable sampling
    /// - Auto-scaling prevents data clipping
    /// 
    /// Cons:
    /// - Custom rendering adds complexity
    /// - Limited to simple line graphs
    /// - Requires manual styling integration
    /// </summary>
    public class GraphElement : VisualElement
    {
        private CircularBuffer<float> _dataPoints;
        private float _minValue = float.MaxValue;
        private float _maxValue = float.MinValue;
        private Color _lineColor = Color.green;
        private Color _backgroundColor = new Color(0, 0, 0, 0.3f);
        private float _lineWidth = 2f;
        private bool _autoScale = true;
        private float _fixedMinValue = 0f;
        private float _fixedMaxValue = 100f;
        
        public int MaxDataPoints { get; set; }
        public float MinDisplayValue => _autoScale ? _minValue : _fixedMinValue;
        public float MaxDisplayValue => _autoScale ? _maxValue : _fixedMaxValue;

        public float Width
        {
            get => style.width.value.value;
            set
            {
                style.width = value;
                MarkDirtyRepaint();
            }
        }

        public float Height
        {
            get => style.height.value.value;
            set
            {
                style.height = value;
                MarkDirtyRepaint();
            }
        }
        
        public GraphElement(int maxDataPoints = 60)
        {
            MaxDataPoints = maxDataPoints;
            _dataPoints = new CircularBuffer<float>(maxDataPoints);
            
            // Set default styling
            style.width = 600;
            style.height = 180;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = Color.gray;
            style.borderBottomColor = Color.gray;
            style.borderLeftColor = Color.gray;
            style.borderRightColor = Color.gray;
            
            // Enable custom drawing
            generateVisualContent += OnGenerateVisualContent;
        }
        
        /// <summary>
        /// Add a new data point to the graph
        /// </summary>
        public void AddDataPoint(float value)
        {
            _dataPoints.Add(value);
            
            // Update min/max for auto-scaling
            if (_autoScale)
            {
                UpdateMinMax();
            }
            
            // Trigger a redraw
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set the line color for the graph
        /// </summary>
        public void SetLineColor(Color color)
        {
            _lineColor = color;
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set fixed scale range (disables auto-scaling)
        /// </summary>
        public void SetFixedScale(float minValue, float maxValue)
        {
            _autoScale = false;
            _fixedMinValue = minValue;
            _fixedMaxValue = maxValue;
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Enable auto-scaling based on data range
        /// </summary>
        public void EnableAutoScale()
        {
            _autoScale = true;
            UpdateMinMax();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Clear all data points
        /// </summary>
        public void Clear()
        {
            _dataPoints.Clear();
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            MarkDirtyRepaint();
        }
        
        private void UpdateMinMax()
        {
            if (_dataPoints.Count == 0) return;
            
            _minValue = _dataPoints.Min();
            _maxValue = _dataPoints.Max();
            
            // Add some padding to prevent flat lines
            float range = _maxValue - _minValue;
            if (range < 0.1f) // Very small range
            {
                float center = (_maxValue + _minValue) / 2f;
                _minValue = center - 0.05f;
                _maxValue = center + 0.05f;
            }
            else
            {
                float padding = range * 0.1f;
                _minValue -= padding;
                _maxValue += padding;
            }
        }
        
        /// <summary>
        /// Custom drawing method called by Unity's UI system
        /// </summary>
        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_dataPoints.Count < 2) return;
            
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;
            
            // Draw background
            DrawBackground(context, rect);
            
            // Draw the line graph
            DrawLineGraph(context, rect);
        }
        
        private void DrawBackground(MeshGenerationContext context, Rect rect)
        {
            var painter = context.painter2D;
            painter.fillColor = _backgroundColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(0, rect.height));
            painter.ClosePath();
            painter.Fill();
        }
        
        private void DrawLineGraph(MeshGenerationContext context, Rect rect)
        {
            var painter = context.painter2D;
            painter.strokeColor = _lineColor;
            painter.lineWidth = _lineWidth;
            
            float minVal = MinDisplayValue;
            float maxVal = MaxDisplayValue;
            float valueRange = maxVal - minVal;
            
            if (valueRange <= 0) return;
            
            painter.BeginPath();
            
            bool firstPoint = true;
            for (int i = 0; i < _dataPoints.Count; i++)
            {
                float value = _dataPoints[i];
                
                // Calculate position
                float x = (i / (float)(_dataPoints.Count - 1)) * rect.width;
                float normalizedValue = (value - minVal) / valueRange;
                float y = rect.height - (normalizedValue * rect.height); // Invert Y axis
                
                Vector2 point = new Vector2(x, y);
                
                if (firstPoint)
                {
                    painter.MoveTo(point);
                    firstPoint = false;
                }
                else
                {
                    painter.LineTo(point);
                }
            }
            
            painter.Stroke();
        }
    }
    
    /// <summary>
    /// Efficient circular buffer implementation for storing graph data
    /// </summary>
    public class CircularBuffer<T>
    {
        private T[] _buffer;
        private int _head = 0;
        private int _count = 0;
        
        public int Capacity { get; private set; }
        public int Count => _count;
        
        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
            _buffer = new T[capacity];
        }
        
        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % Capacity;
            
            if (_count < Capacity)
                _count++;
        }
        
        public T this[int index]
        {
            get
            {
                if (index >= _count)
                    throw new System.IndexOutOfRangeException();
                    
                int actualIndex = (_head - _count + index + Capacity) % Capacity;
                return _buffer[actualIndex];
            }
        }
        
        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
        
        public IEnumerable<T> GetEnumerable()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }
        
        public T Min() => GetEnumerable().Min();
        public T Max() => GetEnumerable().Max();
    }
}
