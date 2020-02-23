using AigisCutter.Model;
using Microsoft.VisualBasic.FileIO;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AigisCutter.ViewModel
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public Version Version { get; set; }

        private CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        public List<ZoomSet> ZoomSet { get; }
            = new List<ZoomSet>()
            {
                new ZoomSet(150),
                new ZoomSet(125),
                new ZoomSet(110),
                new ZoomSet(100),
                new ZoomSet(90),
                new ZoomSet(75),
                new ZoomSet(50),
            };

        public ReactiveCollection<string> FilePaths { get; } = new ReactiveCollection<string>();
        public ReactiveProperty<ZoomSet> SelectedZoom { get; } = new ReactiveProperty<ZoomSet>(new ZoomSet(100));
        public ReactiveProperty<int> Width { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> Height { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> Delta { get; } = new ReactiveProperty<int>(6);
        public ReactiveProperty<int> HomebarHeight { get; } = new ReactiveProperty<int>(32);

        public ReactiveProperty<bool> IsPcSq { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> IsPcEdge { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> IsIosAndroid { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> IsIos { get; } = new ReactiveProperty<bool>(false);

        public ReactiveProperty<Visibility> VisiblitySizeOption { get; }
        public ReactiveProperty<Visibility> VisiblityDeltaOption { get; }
        public ReactiveProperty<Visibility> VisiblityHomebarOption { get; }

        public ReactiveProperty<bool> CanProcessing { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<long> Progress { get; } = new ReactiveProperty<long>(0);

        public AsyncReactiveCommand ExecuteCommand { get; }
        public AsyncReactiveCommand ClearCommand { get; }
        public AsyncReactiveCommand TrushCommand { get; }
        public ReactiveCommand CancelCommand { get; }

        public ICollection<string> errorLog;

        public event Action<string> OnError;

        public async Task AddFilePathsAsync(IEnumerable<string> paths)
        {
            CanProcessing.Value = false;

            await Task.Run(() =>
            {
                foreach (var path in paths)
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    if (FilePaths.Contains(path) || !File.Exists(path))
                        continue;

                    try
                    {
                        using (var bmp = new Bitmap(path))
                        { }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    FilePaths.AddOnScheduler(path);
                }
            }, CancellationToken);

            CanProcessing.Value = true;
        }

        private async Task ExecuteAsync()
        {
            var ct = CancellationToken;

            await Task.Run(() =>
            {
                Progress.Value = 0;
                int count = 0;

                var outputDirectoryPath =
                    Path.GetDirectoryName(FilePaths.First())
                    + "\\AigisCutter_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (Directory.Exists(outputDirectoryPath))
                    throw new Exception();

                try
                {
                    Directory.CreateDirectory(outputDirectoryPath);
                }
                catch (Exception)
                {
                    OnError?.Invoke($"出力ディレクトリの作成に失敗しました\r\n{outputDirectoryPath}");
                }

                errorLog = new List<string>();
                Progress.Value = 10;

                try
                {
                    foreach (var path in FilePaths)
                    {
                        ct.ThrowIfCancellationRequested();
                        Progress.Value = 10 + 90 * count++ / FilePaths.Count;

                        var outputPath = outputDirectoryPath
                            + "\\"
                            + Path.GetFileNameWithoutExtension(path)
                            + ".png";

                        try
                        {
                            if (IsPcSq.Value)
                                GameCutter.CutPcCount(outputPath, path, Width.Value, Height.Value, Delta.Value);
                            else if (IsPcEdge.Value)
                                GameCutter.CutPcSq(outputPath, path, Width.Value, Height.Value);
                            else if (IsIos.Value)
                                GameCutter.CutIosPoint(outputPath, path, HomebarHeight.Value);
                            else if (IsIosAndroid.Value)
                                GameCutter.CutIosAndroid(outputPath, path, Delta.Value);
                            else
                                throw new InvalidOperationException();
                        }
                        catch (FileNotFoundException)
                        {
                            errorLog.Add($"{path}が見つかりませんでした");
                        }
                        catch (Exception ex)
                        {
                            errorLog.Add($"{path} {ex.Message.Replace("\r\n", "  ")}");
                        }
                    }
                    Progress.Value = 100;
                }
                catch (OperationCanceledException)
                {
                }

                if (errorLog.Count > 0)
                {
                    try
                    {
                        var logPath = outputDirectoryPath + "\\error.txt";
                        using (var sw = new StreamWriter(logPath, false, Encoding.GetEncoding("Shift_JIS")))
                        {
                            foreach (var e in errorLog)
                                sw.WriteLine(e);
                        }
                        OnError?.Invoke($"{errorLog.Count}枚の画像でエラーが発生しました\r\n{logPath}を確認してください");
                    }
                    catch (Exception)
                    {
                        OnError?.Invoke("予期せぬエラーが発生しました");
                    }
                }
            }, ct).ConfigureAwait(false);
        }

        private async Task TrushASync()
        {
            await Task.Run(() =>
            {
                foreach (var path in FilePaths)
                {
                    if (!File.Exists(path))
                        continue;

                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }

                FilePaths.ClearOnScheduler();
            }, CancellationToken).ConfigureAwait(false);
        }

        private async Task ClearAsync()
        {
            await Task.Run(() =>
            {
                FilePaths.ClearOnScheduler();
            }, CancellationToken).ConfigureAwait(false);
        }

        public MainWindowModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"アイギスCutter {Version.Major}.{Version.Minor}.{Version.Build}";

            //コマンド生成
            var temp = FilePaths.ToCollectionChanged()
                .CombineLatest(CanProcessing, (f, p) => p && FilePaths.Count > 0);

            ExecuteCommand = temp.ToAsyncReactiveCommand(CanProcessing)
                .WithSubscribe(ExecuteAsync);
            ClearCommand = temp.ToAsyncReactiveCommand(CanProcessing)
                .WithSubscribe(ClearAsync);
            TrushCommand = temp.ToAsyncReactiveCommand(CanProcessing)
                .WithSubscribe(TrushASync);
            CancelCommand = CanProcessing
                .Select(x => !x)
                .ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    CancellationTokenSource.Cancel();
                    CancellationTokenSource = new CancellationTokenSource();
                });

            CanProcessing
                .Where(x => !x)
                .Subscribe(_ => Progress.Value = 0);

            VisiblitySizeOption = IsPcSq
                .CombineLatest(IsPcEdge, (f, s) => f || s)
                .Select(x => x ? Visibility.Visible : Visibility.Hidden)
                .ToReactiveProperty();
            VisiblityDeltaOption = IsPcSq
                .CombineLatest(IsIosAndroid, (f, s) => f || s)
                .Select(x => x ? Visibility.Visible : Visibility.Hidden)
                .ToReactiveProperty();
            VisiblityHomebarOption = IsIos
                .Select(x => x ? Visibility.Visible : Visibility.Hidden)
                .ToReactiveProperty();

            SelectedZoom
                .Subscribe(s =>
                {
                    Width.Value = s.Width;
                    Height.Value = s.Height;
                });
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}