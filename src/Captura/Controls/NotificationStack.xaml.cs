﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Captura
{
    public partial class NotificationStack
    {
        static readonly TimeSpan TimeoutToHide = TimeSpan.FromSeconds(5);
        DateTime _lastMouseMoveTime;
        readonly DispatcherTimer _timer;

        public NotificationStack()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += TimerOnTick;

            _timer.Start();
        }

        void TimerOnTick(object Sender, EventArgs Args)
        {
            var elapsed = DateTime.Now - _lastMouseMoveTime;

            if (elapsed >= TimeoutToHide)
            {
                var unfinished = ItemsControl.Items
                    .OfType<ProgressBalloon>()
                    .Any(M => !M.ViewModel.Finished);

                if (!unfinished)
                {
                    Hide();
                }
                else _lastMouseMoveTime = DateTime.Now;
            }
        }

        public void Hide()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(100))));

            if (_timer.IsEnabled)
                _timer.Stop();
        }

        public void Show()
        {
            _lastMouseMoveTime = DateTime.Now;

            BeginAnimation(OpacityProperty, new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(300))));

            if (!_timer.IsEnabled)
                _timer.Start();
        }

        void OnClose()
        {
            Hide();

            var copy = ItemsControl.Items.OfType<FrameworkElement>().ToArray();

            foreach (var frameworkElement in copy)
            {
                Remove(frameworkElement);
            }
        }

        void CloseButton_Click(object Sender, RoutedEventArgs E) => OnClose();

        void Remove(FrameworkElement Element)
        {
            var anim = new DoubleAnimation(Element.ActualHeight, 0, new Duration(TimeSpan.FromMilliseconds(200)));

            anim.Completed += (S, E) =>
            {
                ItemsControl.Items.Remove(Element);

                if (ItemsControl.Items.Count == 0)
                {
                    Hide();
                }
            };

            Element.BeginAnimation(HeightProperty, anim);
        }

        const int MaxItems = 5;

        public void Add(FrameworkElement Element)
        {
            if (Element is IRemoveRequester removeRequester)
            {
                removeRequester.RemoveRequested += () => Remove(Element);
            }

            if (Element is ScreenShotBalloon ssBalloon)
                ssBalloon.Expander.IsExpanded = true;

            foreach (var item in ItemsControl.Items)
            {
                if (item is ScreenShotBalloon screenShotBalloon)
                {
                    screenShotBalloon.Expander.IsExpanded = false;
                }
            }

            ItemsControl.Items.Insert(0, Element);

            if (ItemsControl.Items.Count > MaxItems)
            {
                var itemsToRemove = ItemsControl.Items
                    .OfType<FrameworkElement>()
                    .Skip(MaxItems)
                    .ToArray();

                foreach (var frameworkElement in itemsToRemove)
                {
                    if (frameworkElement is ProgressBalloon progressBalloon && !progressBalloon.ViewModel.Finished)
                        continue;

                    Remove(frameworkElement);
                }
            }
        }

        void NotificationStack_OnMouseMove(object Sender, MouseEventArgs E)
        {
            if (ItemsControl.Items.Count == 0)
                return;

            _lastMouseMoveTime = DateTime.Now;

            Show();
        }
    }
}