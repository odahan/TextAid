using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using TextAid.App.ViewModels;

namespace TextAid.Platform.Windows.Tests;

public sealed class ModelSelectorTests
{
    [Fact]
    public void ThemedModelSelector_OpensWithVisibleItemsAndCommitsMouseSelection()
    {
        RunInSta(() =>
        {
            var viewModel = new SettingsViewModel { NetworkModel = "first:latest" };
            viewModel.NetworkModels.Add("first:latest");
            viewModel.NetworkModels.Add("second:latest");
            var selector = new ComboBox { Width = 300, VerticalAlignment = VerticalAlignment.Top, DataContext = viewModel };
            selector.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(SettingsViewModel.NetworkModels)));
            selector.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(SettingsViewModel.NetworkModel)) { Mode = BindingMode.TwoWay });
            var window = new Window { Content = new ScrollViewer { Content = selector }, Width = 400, Height = 300, ShowInTaskbar = false };
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (string dictionary in new[] { "Colors", "Brushes", "Controls" })
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/TextAid;component/Themes/{dictionary}.xaml", UriKind.Relative) });

            try
            {
                window.Show();
                selector.ApplyTemplate();
                var popup = Assert.IsType<Popup>(selector.Template.FindName("PART_Popup", selector));
                int opened = 0;
                int closed = 0;
                selector.DropDownOpened += (_, _) => opened++;
                selector.DropDownClosed += (_, _) => closed++;
                selector.IsDropDownOpen = true;
                FlushLayout();
                Assert.True(popup.IsOpen);
                Assert.Equal(1, opened);
                var item = Assert.IsType<ComboBoxItem>(selector.ItemContainerGenerator.ContainerFromIndex(1));
                Assert.True(item.ActualHeight > 0);
                Assert.True(popup.Child.RenderSize.Height > selector.ActualHeight);
                item.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent });
                FlushLayout();
                Assert.Equal("second:latest", viewModel.NetworkModel);
                Assert.False(selector.IsDropDownOpen);
                Assert.False(popup.IsOpen);
                Assert.Equal(1, closed);
            }
            finally
            {
                window.Close();
                application.Shutdown();
            }
        });
    }

    [Fact]
    public async Task FailedNetworkDiscovery_PreservesTheCurrentModelAndChoices()
    {
        var viewModel = new SettingsViewModel { NetworkEndpoint = "", NetworkModel = "saved:latest" };
        viewModel.NetworkModels.Add("saved:latest");
        await viewModel.LoadNetworkModelsCommand.ExecuteAsync(null);
        Assert.Equal(ConnectionTestState.Failed, viewModel.NetworkConnectionState);
        Assert.Equal("saved:latest", viewModel.NetworkModel);
        Assert.Equal(new[] { "saved:latest" }, viewModel.NetworkModels);
    }

    private static void FlushLayout() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The model selector test did not finish.");
        if (failure is not null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }
}
