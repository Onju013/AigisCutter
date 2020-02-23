using AigisCutter.ViewModel;
using System.Windows;

namespace AigisCutter.View
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowModel Vm => DataContext as MainWindowModel;

        public MainWindow()
        {
            InitializeComponent();

            Vm.OnError += OnError;
        }

        private void OnError(string message)
        {
            MessageBox.Show(
                message,
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.Args.Count > 0)
                await Vm.AddFilePathsAsync(App.Args).ConfigureAwait(false);
        }

        private void Window_DragOver(
            object sender,
            DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(
            object sender,
            DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
                return;

            await Vm.AddFilePathsAsync(files).ConfigureAwait(false);
        }
    }
}