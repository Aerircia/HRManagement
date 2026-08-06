using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace HRManagement.Utilities
{
    public static class SmoothScrollBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        private static readonly Dictionary<ScrollViewer, ScrollAnimationHelper> Helpers = new();

        private static void OnIsEnabledChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer viewer)
                return;

            if ((bool)e.NewValue)
            {
                if (!Helpers.ContainsKey(viewer))
                {
                    Helpers[viewer] = new ScrollAnimationHelper(viewer);
                    viewer.PreviewMouseWheel += Helpers[viewer].OnMouseWheel;
                }
            }
            else
            {
                if (Helpers.TryGetValue(viewer, out var helper))
                {
                    viewer.PreviewMouseWheel -= helper.OnMouseWheel;
                    Helpers.Remove(viewer);
                }
            }
        }

        private class ScrollAnimationHelper : Animatable
        {
            private readonly ScrollViewer _viewer;

            protected override Freezable CreateInstanceCore()
            {
                return new ScrollAnimationHelper(_viewer);
            }

            public ScrollAnimationHelper(ScrollViewer viewer)
            {
                _viewer = viewer;
            }

            #region AnimatedOffset

            public double AnimatedOffset
            {
                get => (double)GetValue(AnimatedOffsetProperty);
                set => SetValue(AnimatedOffsetProperty, value);
            }

            public static readonly DependencyProperty AnimatedOffsetProperty =
                DependencyProperty.Register(
                    nameof(AnimatedOffset),
                    typeof(double),
                    typeof(ScrollAnimationHelper),
                    new PropertyMetadata(0.0, OnAnimatedOffsetChanged));

            private static void OnAnimatedOffsetChanged(
                DependencyObject d,
                DependencyPropertyChangedEventArgs e)
            {
                var helper = (ScrollAnimationHelper)d;
                helper._viewer.ScrollToVerticalOffset((double)e.NewValue);
            }

            #endregion

            public void OnMouseWheel(object sender, MouseWheelEventArgs e)
            {
                e.Handled = true;

                double target = AnimatedOffset - e.Delta;

                if (target < 0)
                    target = 0;

                if (target > _viewer.ScrollableHeight)
                    target = _viewer.ScrollableHeight;

                var animation = new DoubleAnimation
                {
                    To = target,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseOut
                    }
                };

                BeginAnimation(AnimatedOffsetProperty, animation, HandoffBehavior.Compose);
            }
        }
    }
}
