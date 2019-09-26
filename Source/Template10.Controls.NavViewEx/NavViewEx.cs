﻿using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Collections.ObjectModel;
using win = Windows;
using System.Threading;
using Template10.Navigation;
using Template10.Services;

namespace Template10.Controls
{
    public class NavViewEx : NavigationView
    {
        private Button _togglePaneButton;
        private TextBlock _paneTitleTextBlock;
        private Button _backButton;
        private readonly CoreDispatcher _dispatcher;
        private readonly Frame _frame;

        public NavViewEx()
        {
            DefaultStyleKey = typeof(NavigationView);

            if (win.ApplicationModel.DesignMode.DesignModeEnabled
                || win.ApplicationModel.DesignMode.DesignMode2Enabled)
            {
                return;
            }

            Content = _frame = new Frame();
            _dispatcher = _frame.Dispatcher;

            _frame.Navigated += (s, e) =>
            {
                if (TryFindItem(e.SourcePageType, e.Parameter, out var item))
                {
                    SetSelectedItem(item);
                }
            };

            NavigationService = NavigationFactory
                .Create(_frame)
                .AttachGestures(Window.Current, Gesture.Back, Gesture.Forward, Gesture.Refresh);

            ItemInvoked += (s, e) =>
            {
                SelectedItem = (e.IsSettingsInvoked) ? SettingsItem : Find(e.InvokedItem.ToString());
            };

            RegisterPropertyChangedCallback(IsPaneOpenProperty, (s, e) =>
            {
                UpdateAppTitleVisibility();
                UpdatePaneHeadersVisibility();
            });

            Window.Current.CoreWindow.SizeChanged += (s, e) =>
            {
                UpdatePageHeaderContent();
            };

            Loaded += (s, e) =>
            {
                UpdateAppTitleVisibility();
                UpdatePaneHeadersVisibility();
                UpdatePageHeaderContent();
                SetupBackButton();
            };
        }

        public INavigationService NavigationService { get; }

        private void SetupBackButton()
        {
            var children = XamlUtilities.RecurseChildren(this);
            var grids = children.OfType<Grid>();
            var grid = grids.Single(x => x.Name == "TogglePaneTopPadding");
            grid.Visibility = Visibility.Collapsed;

            grid = grids.Single(x => x.Name == "ContentPaneTopPadding");
            grid.RegisterPropertyChangedCallback(HeightProperty, (s, args) =>
            {
                if (grid.Height != 44d)
                {
                    grid.Height = 44d;
                }
            });
            grid.Height = 44d;

            var child_buttons = children.OfType<Button>();

            _togglePaneButton = child_buttons.Single(x => x.Name == "TogglePaneButton");
            _togglePaneButton.RegisterPropertyChangedCallback(MarginProperty, (s, args) =>
            {
                if (_togglePaneButton.Margin.Top != 0)
                {
                    _togglePaneButton.Margin = new Thickness(0, 0, 0, 32);
                }
            });
            _togglePaneButton.Margin = new Thickness(0, 0, 0, 32);
            _togglePaneButton.Focus(FocusState.Programmatic);

            var parent_grid = _togglePaneButton.Parent as Grid;
            parent_grid.Width = double.NaN;
            parent_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            parent_grid.ColumnDefinitions.Add(new ColumnDefinition { });
            parent_grid.RowDefinitions[0].Height = GridLength.Auto;
            parent_grid.RowDefinitions[1].Height = GridLength.Auto;

            _paneTitleTextBlock = new TextBlock
            {
                Name = "PaneTitleTextBlock",
                Margin = new Thickness(8, 18, 0, 0),
                FontSize = 15,
                FontFamily = new FontFamily("Segoe UI Bold"),
                TextWrapping = TextWrapping.NoWrap,
                Foreground = Resources["SystemControlForegroundBaseHighBrush"] as Brush,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false,
                Text = "Jerry Nixon",
            };
            _paneTitleTextBlock.SetValue(Grid.ColumnProperty, 1);
            _paneTitleTextBlock.SetValue(Grid.RowProperty, 1);
            _paneTitleTextBlock.SetValue(Canvas.ZIndexProperty, 100);
            // parent_grid.Children.Add(_paneTitleTextBlock);

            _backButton = new Button
            {
                Name = "BackButton",
                Content = new SymbolIcon
                {
                    Symbol = Symbol.Back,
                    IsHitTestVisible = false
                },
                Style = Resources["PaneToggleButtonStyle"] as Style,
            };
            _backButton.SetValue(Canvas.ZIndexProperty, 100);
            parent_grid.Children.Insert(1, _backButton);

            NavigationService.CanGoBackChanged += (s, args) =>
            {
                _backButton.IsEnabled = NavigationService.CanGoBack();
            };

            _backButton.Click += (s, args) =>
            {
                var gesture_service = GestureService.GetForCurrentView();
                gesture_service.RaiseBackRequested();
            };
        }

        public INavigationService Initialize()
        {
            var first = MenuItems
                .OfType<NavigationViewItem>()
                .SingleOrDefault(x => (bool)x.GetValue(NavViewProps.IsStartPageProperty));

            if (first != null)
            {
                SetSelectedItem(first);
            }

            return NavigationService;
        }

        public enum ItemHeaderBehaviors { Hide, Remove, None }
        public ItemHeaderBehaviors ItemHeaderBehavior { get; set; } = ItemHeaderBehaviors.Remove;

        public Uri SettingsNavigationUri { get; set; }
        public event EventHandler SettingsInvoked;

        public new object SelectedItem
        {
            set => SetSelectedItem(value);
            get => base.SelectedItem;
        }

        private async void SetSelectedItem(object selectedItem)
        {
            if (selectedItem == null)
            {
                base.SelectedItem = null;
            }
            else if (selectedItem == base.SelectedItem)
            {
                // already set
            }
            else if (selectedItem == SettingsItem)
            {
                if (SettingsNavigationUri != null)
                {
                    await NavigationService.NavigateAsync(SettingsNavigationUri);
                    base.SelectedItem = selectedItem;
                }
                SettingsInvoked?.Invoke(this, EventArgs.Empty);
            }
            else if (selectedItem is NavigationViewItem item)
            {
                if (item.GetValue(NavViewProps.NavigationUriProperty) is string path)
                {
                    if ((await NavigationService.NavigateAsync(path)).Success)
                    {
                        base.SelectedItem = selectedItem;
                    }
                    else
                    {
                        Debug.WriteLine($"{selectedItem}.{nameof(NavViewProps.NavigationUriProperty)} navigation failed.");
                    }
                }
                else
                {
                    Debug.WriteLine($"{selectedItem}.{nameof(NavViewProps.NavigationUriProperty)} is not valid Uri");
                }
            }
            UpdatePaneHeadersVisibility();
            UpdatePageHeaderContent();
        }

        private void UpdateAppTitleVisibility()
        {
            if (_paneTitleTextBlock != null)
            {
                _paneTitleTextBlock.Visibility = IsPaneOpen
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdatePaneHeadersVisibility()
        {
            foreach (var item in MenuItems.OfType<NavigationViewItemHeader>())
            {
                switch (ItemHeaderBehavior)
                {
                    case ItemHeaderBehaviors.Hide:
                        item.Opacity = IsPaneOpen ? 1 : 0;
                        break;
                    case ItemHeaderBehaviors.Remove:
                        item.Visibility = IsPaneOpen ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case ItemHeaderBehaviors.None:
                        // empty
                        break;
                }
            }
        }

        private static SemaphoreSlim _updatePageHeaderSemaphore = new SemaphoreSlim(1, 1);

        private void UpdatePageHeaderContent()
        {
            _updatePageHeaderSemaphore.Wait();

            bool localTryGetCommandBar(out CommandBar bar)
            {
                var children = XamlUtilities.RecurseChildren(this);
                var bars = children
                    .OfType<CommandBar>();
                if (!bars.Any())
                {
                    bar = default(CommandBar);
                    return false;
                }
                bar = bars.First();
                return true;
            }

            void localUpdatePageHeaderCommands(ObservableCollection<object> headerCommands)
            {
                if (!localTryGetCommandBar(out var bar))
                {
                    return;
                }

                var previous = bar.PrimaryCommands
                    .OfType<DependencyObject>()
                    .Where(x => x.GetValue(NavViewProps.PageHeaderCommandDynamicItemProperty) is bool value && value);

                foreach (var command in previous.OfType<ICommandBarElement>().ToArray())
                {
                    bar.PrimaryCommands.Remove(command);
                }

                foreach (var command in headerCommands.Reverse().OfType<DependencyObject>().ToArray())
                {
                    command.SetValue(NavViewProps.PageHeaderCommandDynamicItemProperty, true);
                    bar.PrimaryCommands.Insert(0, command as ICommandBarElement);
                }
            }

            try
            {
                if (_frame.Content is Page page)
                {
                    if (page.GetValue(NavViewProps.HeaderTextProperty) is string headerText && !Equals(Header, headerText))
                    {
                        Header = headerText;
                    }

                    if (page.GetValue(NavViewProps.HeaderCommandsProperty) is ObservableCollection<object> headerCommands && headerCommands.Any())
                    {
                        localUpdatePageHeaderCommands(headerCommands);
                    }
                }
            }
            finally
            {
                _updatePageHeaderSemaphore.Release();
            }
        }

        private bool TryFindItem(Type type, object parameter, out object item)
        {
            // registered?

            if (!Navigation.PageRegistry.TryGetRegistration(type, out var info))
            {
                item = null;
                return false;
            }

            // search settings

            if (Navigation.NavigationQueue.TryParse(SettingsNavigationUri, null, out var settings))
            {
                if (type == settings.Last().View && (string)parameter == settings.Last().QueryString)
                {
                    item = SettingsItem;
                    return true;
                }
                else
                {
                    // not settings
                }
            }

            // filter menu items

            var menuItems = MenuItems
                .OfType<NavigationViewItem>()
                .Select(x => new
                {
                    Item = x,
                    Path = x.GetValue(NavViewProps.NavigationUriProperty) as string
                })
                .Where(x => !string.IsNullOrEmpty(x.Path));

            // search filtered items

            foreach (var menuItem in menuItems)
            {
                if (Navigation.NavigationQueue.TryParse(menuItem.Path, null, out var menuQueue)
                    && Equals(menuQueue.Last().View, type))
                {
                    item = menuItem;
                    return true;
                }
            }

            // not found

            item = null;
            return false;
        }

        private NavigationViewItem Find(string content)
        {
            return MenuItems.OfType<NavigationViewItem>().SingleOrDefault(x => x.Content.Equals(content));
        }
    }
}
