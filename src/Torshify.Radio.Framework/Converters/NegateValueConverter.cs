﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Torshify.Radio.Framework.Converters
{
    /// <summary>
    /// Produces an output value that is the negative of the input.
    /// </summary>
    /// <remarks>
    /// The built-in signed types are supported as well as a handful of other
    /// commonly used types such as <see cref="T:Point"/>, <see cref="T:TimeSpan"/>,
    /// <see cref="T:Thickness"/>, etc.
    /// </remarks>
    public sealed class NegateConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// The default singleton instance of this converter.
        /// </summary>
        public static readonly NegateConverter Default = new NegateConverter();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NegateConverter"/> class.
        /// </summary>
        public NegateConverter()
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value is double)
            {
                return Negate((double)value);
            }

            if (value is int)
            {
                return Negate((int)value);
            }

            if (value is bool)
            {
                return Negate((bool)value);
            }

            if (value is long)
            {
                return Negate((long)value);
            }

            if (value is IConvertible)
            {
                return Negate((IConvertible)value, culture);
            }

            if (value is TimeSpan)
            {
                return Negate((TimeSpan)value);
            }

            if (value is Point)
            {
                return Negate((Point)value);
            }

            if (value is Thickness)
            {
                return Negate((Thickness)value);
            }

            throw new ArgumentException("Cannot negate " + value.GetType() + ".", "value");
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        /// <summary>
        /// Negates a <see cref="T:TimeSpan"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static TimeSpan Negate(TimeSpan value)
        {
            return value.Negate();
        }

        /// <summary>
        /// Negates a <see cref="T:Point"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static Point Negate(Point value)
        {
            return new Point(
                -value.X,
                -value.Y
            );
        }

        /// <summary>
        /// Negates a <see cref="T:Thickness"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static Thickness Negate(Thickness value)
        {
            return new Thickness(
                -value.Left,
                -value.Top,
                -value.Right,
                -value.Bottom
            );
        }

        /// <summary>
        /// Negates a <see cref="T:Boolean"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static bool Negate(bool value)
        {
            return !value;
        }

        /// <summary>
        /// Negates a <see cref="T:Int32"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static int Negate(int value)
        {
            return -value;
        }

        /// <summary>
        /// Negates a <see cref="T:Int64"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static long Negate(long value)
        {
            return -value;
        }

        /// <summary>
        /// Negates a <see cref="T:Double"/> value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The negated value.</returns>
        private static double Negate(double value)
        {
            return -value;
        }

        /// <summary>
        /// Negates a <see cref="T:IConvertible"/> value by round tripping through
        /// the System.Decimal type.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <param name="formatProvider">The culture information.</param>
        /// <returns>The negated value as the original input type.</returns>
        private static object Negate(IConvertible value, IFormatProvider formatProvider)
        {
            TypeCode inputType = value.GetTypeCode();

            decimal input = value.ToDecimal(formatProvider);
            decimal output = Decimal.Negate(input);

            return System.Convert.ChangeType(output, inputType, formatProvider);
        }

        #endregion Methods
    }
}