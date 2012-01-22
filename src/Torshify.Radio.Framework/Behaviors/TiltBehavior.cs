﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

using Torshify.Radio.Framework.Controls;

namespace Torshify.Radio.Framework.Behaviors
{
    public class TiltBehavior : Behavior<FrameworkElement>
    {
        #region Fields

        public static readonly DependencyProperty KeepDraggingProperty = 
            DependencyProperty.Register("KeepDragging", typeof(bool), typeof(TiltBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty TiltFactorProperty = 
            DependencyProperty.Register("TiltFactor", typeof(Int32), typeof(TiltBehavior), new PropertyMetadata(20));

        private FrameworkElement _attachedElement;
        private Point _current = new Point(-99, -99);
        private bool _isPressed = false;
        private Thickness _originalMargin;
        private Panel _originalPanel;
        private Size _originalSize;
        private Int32 _times = -1;

        #endregion Fields

        #region Properties

        public bool KeepDragging
        {
            get { return (bool)GetValue(KeepDraggingProperty); }
            set { SetValue(KeepDraggingProperty, value); }
        }

        public Planerator RotatorParent
        {
            get; set;
        }

        public Int32 TiltFactor
        {
            get { return (Int32)GetValue(TiltFactorProperty); }
            set { SetValue(TiltFactorProperty, value); }
        }

        #endregion Properties

        #region Methods

        protected override void OnAttached()
        {
            _attachedElement = this.AssociatedObject;

            if (_attachedElement as Panel != null)
            {
                (_attachedElement as Panel).Loaded += (sl, el) =>
                                                         {
                                                             List<UIElement> elements = new List<UIElement>();

                                                             foreach (UIElement ui in (_attachedElement as Panel).Children)
                                                             {
                                                                 elements.Add(ui);
                                                             }
                                                             elements.ForEach((element) => Interaction.GetBehaviors(element).Add(new TiltBehavior() { KeepDragging = this.KeepDragging, TiltFactor = this.TiltFactor }));
                                                         };

                return;
            }

            _originalPanel = _attachedElement.Parent as Panel;
            _originalMargin = _attachedElement.Margin;
            _originalSize = new Size(_attachedElement.Width, _attachedElement.Height);
            double left = Canvas.GetLeft(_attachedElement);
            double right = Canvas.GetRight(_attachedElement);
            double top = Canvas.GetTop(_attachedElement);
            double bottom = Canvas.GetBottom(_attachedElement);
            int z = Canvas.GetZIndex(_attachedElement);
            VerticalAlignment va = _attachedElement.VerticalAlignment;
            HorizontalAlignment ha = _attachedElement.HorizontalAlignment;

            #region Setting Container Properties
            RotatorParent = new Planerator();
            RotatorParent.Margin = _originalMargin;
            RotatorParent.Width = _originalSize.Width;
            RotatorParent.Height = _originalSize.Height;
            RotatorParent.VerticalAlignment = va;
            RotatorParent.HorizontalAlignment = ha;
            RotatorParent.SetValue(Canvas.LeftProperty, left);
            RotatorParent.SetValue(Canvas.RightProperty, right);
            RotatorParent.SetValue(Canvas.TopProperty, top);
            RotatorParent.SetValue(Canvas.BottomProperty, bottom);
            RotatorParent.SetValue(Canvas.ZIndexProperty, z);
            #endregion

            #region Removing Child Properties
            _originalPanel.Children.Remove(_attachedElement);
            _attachedElement.Margin = new Thickness();
            _attachedElement.Width = double.NaN;
            _attachedElement.Height = double.NaN;
            #endregion

            _originalPanel.Children.Add(RotatorParent);
            RotatorParent.Child = _attachedElement;

            CompositionTarget.Rendering += new EventHandler(CompositionTargetRendering);
        }

        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            if (KeepDragging)
            {
                _current = Mouse.GetPosition(RotatorParent.Child);
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    if (_current.X > 0 && _current.X < (_attachedElement as FrameworkElement).ActualWidth && _current.Y > 0 && _current.Y < (_attachedElement as FrameworkElement).ActualHeight)
                    {
                        RotatorParent.RotationY = -1 * TiltFactor + _current.X * 2 * TiltFactor / (_attachedElement as FrameworkElement).ActualWidth;
                        RotatorParent.RotationX = -1 * TiltFactor + _current.Y * 2 * TiltFactor / (_attachedElement as FrameworkElement).ActualHeight;
                    }
                }
                else
                {
                    RotatorParent.RotationY = RotatorParent.RotationY - 5 < 0 ? 0 : RotatorParent.RotationY - 5;
                    RotatorParent.RotationX = RotatorParent.RotationX - 5 < 0 ? 0 : RotatorParent.RotationX - 5;
                }
            }
            else
            {

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {

                    if (!_isPressed)
                    {
                        _current = Mouse.GetPosition(RotatorParent.Child);
                        if (_current.X > 0 && _current.X < (_attachedElement as FrameworkElement).ActualWidth && _current.Y > 0 && _current.Y < (_attachedElement as FrameworkElement).ActualHeight)
                        {
                            RotatorParent.RotationY = -1 * TiltFactor + _current.X * 2 * TiltFactor / (_attachedElement as FrameworkElement).ActualWidth;
                            RotatorParent.RotationX = -1 * TiltFactor + _current.Y * 2 * TiltFactor / (_attachedElement as FrameworkElement).ActualHeight;
                        }
                        _isPressed = true;
                    }

                    if (_isPressed && _times == 7)
                    {
                        RotatorParent.RotationY = RotatorParent.RotationY - 5 < 0 ? 0 : RotatorParent.RotationY - 5;
                        RotatorParent.RotationX = RotatorParent.RotationX - 5 < 0 ? 0 : RotatorParent.RotationX - 5;
                    }
                    else if (_isPressed && _times < 7)
                    {
                        _times++;
                    }
                }
                else
                {
                    _isPressed = false;
                    _times = -1;
                    RotatorParent.RotationY = RotatorParent.RotationY - 5 < 0 ? 0 : RotatorParent.RotationY - 5;
                    RotatorParent.RotationX = RotatorParent.RotationX - 5 < 0 ? 0 : RotatorParent.RotationX - 5;
                }

            }
        }

        #endregion Methods
    }
}