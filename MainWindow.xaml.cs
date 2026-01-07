using LiraMosaicViewer.Core;
using LiraMosaicViewer.Models;
using LiraMosaicViewer.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace LiraMosaicViewer
{
    public partial class MainWindow : Window
    {
        private readonly MosaicSceneBuilder _sceneBuilder = new();
        private readonly CultureInfo _uiCulture = CultureInfo.GetCultureInfo("ru-RU");

        private readonly ObservableCollection<ExportSheetItem> _exportQueue = new();

        // Кэши для экспорта (пересборка, но без лишнего чтения с диска)
        private readonly Dictionary<string, Dictionary<int, Element2D>> _geomCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MomentsTable> _momentsCache = new(StringComparer.OrdinalIgnoreCase);
        private bool _exportRunning = false;
        private CancellationTokenSource? _exportCts;


        public MainWindow()
        {
            InitializeComponent();
            // Отчёт: по умолчанию "Моменты"
            ReportComboBox.ItemsSource = new[] { "Моменты", "Перемещения" };
            ReportComboBox.SelectedIndex = 0;

            // RSN пока пустой (заполняем при выборе плиты)
            RsnComboBox.ItemsSource = Array.Empty<int>();

            // Поле заполним под текущий режим
            RebindFieldComboForReport();
            SetReportModeUi();


            MeshCheckBox.Checked += MeshChanged;
            MeshCheckBox.Unchecked += MeshChanged;

            FolderTextBox.Text = @"C:\Temp\тест отчета";
            OutputFolderTextBox.Text = @"C:\Temp";
            StartSheetNumberTextBox.Text = "1";

            ExportQueueListBox.ItemsSource = _exportQueue;

           
            RefreshPairs();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshPairs();

        private void RefreshPairs()
        {
            try
            {
                StatusTextBlock.Text = "";
                ExportStatusTextBlock.Text = "";

                PairsListBox.ItemsSource = null;

                var folder = FolderTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(folder))
                {
                    StatusTextBlock.Text = "Папка не задана.";
                    return;
                }

                LoadLoadCases(folder);

                var pairs = FilePairFinder.FindPairs(folder).ToList();
                PairsListBox.ItemsSource = pairs;

                if (pairs.Count == 0)
                {
                    StatusTextBlock.Text = "Пары не найдены.";
                    ReportView.Scene = null;
                    return;
                }

                PairsListBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка сканирования папки: " + ex.Message;
                ReportView.Scene = null;
            }
        }

        private void PairsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            => LoadSelectedPairAndRender();

        private void Field_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            => LoadSelectedPairAndRender();

        private void LoadCase_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            => LoadSelectedPairAndRender();

        private void MeshChanged(object sender, RoutedEventArgs e)
        {
            ReportView.ShowMesh = MeshCheckBox.IsChecked == true;
            ReportView.InvalidateVisual();
        }

        private void LoadSelectedPairAndRender()
        {

            try
            {
                StatusTextBlock.Text = "";
                ExportStatusTextBlock.Text = "";

                var pair = PairsListBox.SelectedItem as FilePairItem;
                if (pair == null)
                {
                    ReportView.Scene = null;
                    return;
                }


                var field = (MomentField)(FieldComboBox.SelectedItem ?? MomentField.My);

                var geom = GeometryCsvReader.ReadElements(pair.GeomPath);
                var momentsTable = MomentsCsvReader.ReadTables(pair.MomentsPaths);

                var selectedLcItem = LoadCaseComboBox.SelectedItem as LoadCaseItem;
                int lc = selectedLcItem?.Lc ?? 0;

                if (momentsTable.AvailableLCs.Count == 0)
                {
                    StatusTextBlock.Text = "В moments-файлах нет данных.";
                    ReportView.Scene = null;
                    ReportView.Page = new ReportPageModel
                    {
                        LoadTitle = selectedLcItem?.Name ?? "—",
                        MosaicTitle = $"Мозаика напряжений по {field}",
                        UnitsTitle = "Единицы измерения - т/м2",
                        FigureCaption = "Рис. 1 ...",
                        SheetNumber = "1",
                        NoDataText = "Нет данных в таблице"
                    };
                    ReportView.InvalidateVisual();
                    return;
                }

                if (lc == 0)
                    lc = momentsTable.AvailableLCs.OrderBy(x => x).First();

                // Если выбранный LC отсутствует — показываем "Нет данных"
                if (!momentsTable.AvailableLCs.Contains(lc))
                {
                    ReportView.Scene = null;
                    ReportView.Page = new ReportPageModel
                    {
                        LoadTitle = selectedLcItem?.Name ?? $"LC={lc}",
                        MosaicTitle = $"Мозаика напряжений по {field}",
                        UnitsTitle = "Единицы измерения - т/м2",
                        FigureCaption = "Рис. 1 ...",
                        SheetNumber = "1",
                        NoDataText = "Нет данных в таблице"
                    };
                    ReportView.ShowMesh = MeshCheckBox.IsChecked == true;
                    ReportView.InvalidateVisual();

                    StatusTextBlock.Text = $"LC={lc}, {field} | нет данных в таблице моментов";
                    return;
                }

                var valuesByElement = momentsTable.GetValuesByElement(lc, field);
                var scene = _sceneBuilder.Build(pair.BaseName, pair.GeomPath, pair.AnyMomentsPath, lc, field, geom, valuesByElement);

                ReportView.Scene = scene;
                ReportView.ShowMesh = MeshCheckBox.IsChecked == true;

                ReportView.Page = new ReportPageModel
                {
                    LoadTitle = selectedLcItem?.Name ?? $"LC={lc}",
                    MosaicTitle = $"Мозаика напряжений по {field}",
                    UnitsTitle = "Единицы измерения - т/м2",
                    FigureCaption = "Рис. 1 ...",
                    SheetNumber = "1",
                    NoDataText = scene == null ? "Нет данных в таблице" : ""
                };

                ReportView.InvalidateVisual();

                StatusTextBlock.Text = scene == null
                    ? $"LC={lc}, {field} | геом.: {geom.Count}, значений: {valuesByElement.Count}, нет данных для отображения"
                    : $"LC={lc}, {field} | геом.: {geom.Count}, значений: {valuesByElement.Count}, нарисовано: {scene.RenderedElementCount}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка загрузки/отрисовки: " + ex.Message;
                ReportView.Scene = null;
            }
        }

        private void LoadLoadCases(string folder)
        {
            try
            {
                var list = LoadCasesCsvReader.ReadFolder(folder);

                int? keepLc = (LoadCaseComboBox.SelectedItem as LoadCaseItem)?.Lc;

                LoadCaseComboBox.ItemsSource = list;

                if (list.Count == 0)
                {
                    LoadCaseComboBox.SelectedItem = null;
                    return;
                }

                if (keepLc.HasValue)
                {
                    var keep = list.FirstOrDefault(x => x.Lc == keepLc.Value);
                    if (keep != null) { LoadCaseComboBox.SelectedItem = keep; return; }
                }

                LoadCaseComboBox.SelectedIndex = 0;
            }
            catch
            {
                LoadCaseComboBox.ItemsSource = Array.Empty<LoadCaseItem>();
                LoadCaseComboBox.SelectedItem = null;
            }
        }
        private bool IsDisplacementsMode =>
    (ReportComboBox.SelectedItem as string)?.Equals("Перемещения", StringComparison.OrdinalIgnoreCase) == true;

        private void SetReportModeUi()
        {
            // LC активен только для моментов
            LoadCaseComboBox.IsEnabled = !IsDisplacementsMode;

            // RSN активен только для перемещений
            RsnComboBox.IsEnabled = IsDisplacementsMode;
        }

        private void RebindFieldComboForReport()
        {
            if (!IsDisplacementsMode)
            {
                FieldComboBox.ItemsSource = Enum.GetValues(typeof(MomentField));
                if (FieldComboBox.SelectedItem is not MomentField)
                    FieldComboBox.SelectedItem = MomentField.My; // как у тебя по умолчанию было
            }
            else
            {
                FieldComboBox.ItemsSource = Enum.GetValues(typeof(DisplacementField));
                if (FieldComboBox.SelectedItem is not DisplacementField)
                    FieldComboBox.SelectedItem = DisplacementField.Uz;
            }
        }
        private void Report_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RebindFieldComboForReport();
            SetReportModeUi();
            LoadSelectedPairAndRender();
        }

        private void Rsn_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsDisplacementsMode)
                LoadSelectedPairAndRender();
        }



        // =========================
        // Очередь экспорта
        // =========================

        private void AddCurrent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportStatusTextBlock.Text = "";

                var pair = PairsListBox.SelectedItem as FilePairItem;
                if (pair == null) return;

                var lcItem = LoadCaseComboBox.SelectedItem as LoadCaseItem;
                int lc = lcItem?.Lc ?? 0;
                string lcName = lcItem?.Name ?? "";

                if (lc == 0)
                {
                    ExportStatusTextBlock.Text = "Не выбран вариант загружения.";
                    return;
                }

                var field = (MomentField)(FieldComboBox.SelectedItem ?? MomentField.My);

                // Надписи берём из текущего предпросмотра (как ты просил)
                var page = (ReportView.Page ?? new ReportPageModel()).Clone();

                bool isNoData = (ReportView.Scene == null) || !string.IsNullOrWhiteSpace(page.NoDataText);

                _exportQueue.Add(new ExportSheetItem
                {
                    Pair = pair,
                    Lc = lc,
                    LcName = lcName,
                    Field = field,
                    Page = page,
                    ShowMesh = MeshCheckBox.IsChecked == true,
                    IsNoData = isNoData,
                    NoDataReason = isNoData ? (page.NoDataText ?? "") : ""
                });

                ExportStatusTextBlock.Text = $"Добавлено в очередь: {pair.BaseName}, LC={lc}, {field}";
            }
            catch (Exception ex)
            {
                ExportStatusTextBlock.Text = "Ошибка добавления в очередь: " + ex.Message;
            }
        }

        private void AddAllLoadCases_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportStatusTextBlock.Text = "";

                var pair = PairsListBox.SelectedItem as FilePairItem;
                if (pair == null) return;

                var field = (MomentField)(FieldComboBox.SelectedItem ?? MomentField.My);

                // Словарь LC (что показываем в списке)
                var lcList = (LoadCaseComboBox.ItemsSource as IEnumerable<LoadCaseItem>)?.ToList() ?? new List<LoadCaseItem>();
                if (lcList.Count == 0)
                {
                    ExportStatusTextBlock.Text = "Список загружений пуст (loadcases.csv не найден/пуст).";
                    return;
                }

                // Чтобы корректно пометить "нет данных", читаем moments один раз
                var momentsTable = GetMomentsTableCached(pair);

                int added = 0;

                foreach (var lcItem in lcList.OrderBy(x => x.Lc))
                {
                    int lc = lcItem.Lc;
                    bool hasData = momentsTable.AvailableLCs.Contains(lc);

                    // Формируем страницу так же, как в просмотре (надписи заранее)
                    var page = new ReportPageModel
                    {
                        LoadTitle = lcItem.Name,
                        MosaicTitle = $"Мозаика напряжений по {field}",
                        UnitsTitle = "Единицы измерения - т/м2",
                        FigureCaption = "Рис. 1 ...",
                        SheetNumber = "1",
                        NoDataText = hasData ? "" : "Нет данных в таблице"
                    };

                    _exportQueue.Add(new ExportSheetItem
                    {
                        Pair = pair,
                        Lc = lc,
                        LcName = lcItem.Name,
                        Field = field,
                        Page = page,
                        ShowMesh = MeshCheckBox.IsChecked == true,
                        IsNoData = !hasData,
                        NoDataReason = hasData ? "" : "Нет данных в moments"
                    });

                    added++;
                }

                ExportStatusTextBlock.Text = $"Добавлено листов: {added}";
            }
            catch (Exception ex)
            {
                ExportStatusTextBlock.Text = "Ошибка пакетного добавления: " + ex.Message;
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = ExportQueueListBox.SelectedItems.Cast<ExportSheetItem>().ToList();
            if (selected.Count == 0) return;

            foreach (var it in selected)
                _exportQueue.Remove(it);

            ExportStatusTextBlock.Text = $"Удалено из очереди: {selected.Count}";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelected(-1);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelected(+1);
        }

        private void MoveSelected(int delta)
        {
            var selected = ExportQueueListBox.SelectedItems.Cast<ExportSheetItem>().ToList();
            if (selected.Count == 0) return;

            // Чтобы корректно сдвигать группу, сортируем по индексу
            var indices = selected
                .Select(it => _exportQueue.IndexOf(it))
                .Where(i => i >= 0)
                .OrderBy(i => i)
                .ToList();

            if (delta < 0)
            {
                foreach (var i in indices)
                {
                    int ni = i + delta;
                    if (ni < 0) continue;
                    _exportQueue.Move(i, ni);
                }
            }
            else
            {
                for (int k = indices.Count - 1; k >= 0; k--)
                {
                    int i = indices[k];
                    int ni = i + delta;
                    if (ni >= _exportQueue.Count) continue;
                    _exportQueue.Move(i, ni);
                }
            }
        }

        // =========================
        // Экспорт PDF
        // =========================

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_exportRunning)
                return;

            try
            {
                ExportStatusTextBlock.Text = "";
                ExportProgressTextBlock.Text = "";
                ExportProgressBar.Value = 0;

                if (_exportQueue.Count == 0)
                {
                    ExportStatusTextBlock.Text = "Очередь экспорта пуста.";
                    return;
                }

                string outFolder = (OutputFolderTextBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(outFolder))
                {
                    ExportStatusTextBlock.Text = "Не задана папка для PDF.";
                    return;
                }

                if (!int.TryParse((StartSheetNumberTextBox.Text ?? "").Trim(), out int startNo) || startNo <= 0)
                    startNo = 1;

                bool onePdf = OnePdfCheckBox.IsChecked == true;

                int modeIndex = ExportModeComboBox.SelectedIndex;
                if (modeIndex == 1)
                {
                    ExportStatusTextBlock.Text = "Векторная обработка пока не реализована. Выберите растровую.";
                    return;
                }

                int exportDpi = GetSelectedDpi();

                // Снимок очереди (чтобы во время экспорта не зависеть от изменений UI)
                var items = _exportQueue.ToList();

                _exportCts = new CancellationTokenSource();
                var ct = _exportCts.Token;

                SetExportUiState(true);

                // Прогресс: 1 раз на страницу (обновление UI идёт в UI-поток)
                var progress = new Progress<ExportProgress>(p =>
                {
                    ExportProgressBar.Value = Math.Max(0, Math.Min(100, p.Percent));

                    ExportProgressTextBlock.Text =
                        $"{p.Percent:0}%  ({p.Done}/{p.Total})  " +
                        $"Прошло: {FormatTime(p.Elapsed)}  " +
                        $"Осталось: {FormatTime(p.Remaining)}";

                    // можно показывать, что сейчас делаем
                    // ExportStatusTextBlock.Text = p.CurrentTitle;
                });

                // Рендер PNG выполняем через Dispatcher (WPF-визуалы должны жить на UI-потоке)
                Task<byte[]> renderAsync(ExportSheetItem item, int sheetNo, int dpi, CancellationToken token)
                {
                    return Dispatcher.InvokeAsync(
                        () => RenderExportItemToPng(item, sheetNo, dpi),
                        DispatcherPriority.Background,
                        token).Task;
                }

                // Запускаем сам экспорт в фоне (PDF-склейка и сохранение не блокируют окно)
                var result = await Task.Run(() =>
                    ExportRunner.ExportRasterPdfAsync(
                        items: items,
                        startSheetNumber: startNo,
                        dpi: exportDpi,
                        outputFolder: outFolder,
                        onePdf: onePdf,
                        onePdfFileName: "Отчет.pdf",
                        renderPngAsync: renderAsync,
                        separatePdfFileName: (it, sheetNo) => it.BuildSuggestedFileName(sheetNo),
                        progress: progress,
                        ct: ct));

                if (result.OnePdf)
                    ExportStatusTextBlock.Text = "Создан PDF: " + result.SinglePdfPath;
                else
                    ExportStatusTextBlock.Text = $"Создано PDF файлов: {result.PdfPaths.Count} (папка: {outFolder})";

                ExportProgressBar.Value = 100;
            }
            catch (OperationCanceledException)
            {
                ExportStatusTextBlock.Text = "Экспорт отменён.";
            }
            catch (Exception ex)
            {
                ExportStatusTextBlock.Text = "Ошибка экспорта: " + ex.Message;
            }
            finally
            {
                SetExportUiState(false);
                _exportCts?.Dispose();
                _exportCts = null;
            }
        }


        private IEnumerable<Func<byte[]>> MakePageFactories(int startNo, int dpi)
        {
            for (int i = 0; i < _exportQueue.Count; i++)
            {
                int sheetNo = startNo + i;
                var item = _exportQueue[i];

                yield return () => RenderExportItemToPng(item, sheetNo, dpi);
            }
        }

        private IEnumerable<(int sheetNumber, Func<byte[]> makePng, Func<string> fileName)> MakeSeparatePages(int startNo, int dpi)
        {
            for (int i = 0; i < _exportQueue.Count; i++)
            {
                int sheetNo = startNo + i;
                var item = _exportQueue[i];

                yield return (sheetNo,
                    () => RenderExportItemToPng(item, sheetNo, dpi),
                    () => item.BuildSuggestedFileName(sheetNo));
            }
        }

        private byte[] RenderExportItemToPng(ExportSheetItem item, int sheetNo, int dpi)
        {
            // Пересборка сцены из CSV (но геом/моменты кэшируем)
            var pair = item.Pair;

            var geom = GetGeometryCached(pair);
            var moments = GetMomentsTableCached(pair);

            MosaicScene? scene = null;

            if (moments.AvailableLCs.Contains(item.Lc))
            {
                var valuesByElement = moments.GetValuesByElement(item.Lc, item.Field);
                scene = _sceneBuilder.Build(pair.BaseName, pair.GeomPath, pair.AnyMomentsPath, item.Lc, item.Field, geom, valuesByElement);
            }

            // Надписи берем из item.Page (они уже "как на просмотре")
            // Меняем только номер листа
            var page = item.Page.Clone();
            page.SheetNumber = sheetNo.ToString(CultureInfo.InvariantCulture);

            // Если данных нет - оставляем NoDataText
            if (scene == null)
            {
                if (string.IsNullOrWhiteSpace(page.NoDataText))
                    page.NoDataText = "Нет данных в таблице";
            }
            else
            {
                page.NoDataText = ""; // есть сцена - показываем мозаику
            }

            return RenderA4ToPngBytes(page, scene, item.ShowMesh, dpi);
        }

        private Dictionary<int, Element2D> GetGeometryCached(FilePairItem pair)
        {
            if (_geomCache.TryGetValue(pair.GeomPath, out var cached))
                return cached;

            var geom = GeometryCsvReader.ReadElements(pair.GeomPath);
            _geomCache[pair.GeomPath] = geom;
            return geom;
        }

        private MomentsTable GetMomentsTableCached(FilePairItem pair)
        {
            // ключ: плита (BaseName)
            if (_momentsCache.TryGetValue(pair.BaseName, out var cached))
                return cached;

            var table = MomentsCsvReader.ReadTables(pair.MomentsPaths);
            _momentsCache[pair.BaseName] = table;
            return table;
        }

        private static byte[] RenderA4ToPngBytes(ReportPageModel page, MosaicScene? scene, bool showMesh, int dpi)
        {
            // A4 размер в DIU (как в A4ReportView)
            double pageW = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.PageWmm);
            double pageH = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.PageHmm);

            int pixelW = (int)Math.Round(pageW * dpi / 96.0);
            int pixelH = (int)Math.Round(pageH * dpi / 96.0);

            var view = new A4ReportView
            {
                Width = pageW,
                Height = pageH,
                Page = page,
                Scene = scene,
                ShowMesh = showMesh
            };

            view.Measure(new Size(pageW, pageH));
            view.Arrange(new Rect(0, 0, pageW, pageH));
            view.UpdateLayout();

            var rtb = new RenderTargetBitmap(pixelW, pixelH, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(view);

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));

            using var ms = new MemoryStream();
            enc.Save(ms);
            return ms.ToArray();
        }
        private int GetSelectedDpi()
        {
            try
            {
                if (DpiComboBox?.SelectedItem is System.Windows.Controls.ComboBoxItem cbi &&
                    int.TryParse(cbi.Content?.ToString(), out int dpi) &&
                    (dpi == 150 || dpi == 300 || dpi == 600))
                {
                    return dpi;
                }
            }
            catch { }

            return 300; // fallback
        }
        private static string FormatTime(TimeSpan? ts)
        {
            if (ts == null) return "—";
            var t = ts.Value;
            if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
            return t.ToString(@"mm\:ss");
        }

        private void SetExportUiState(bool running)
        {
            _exportRunning = running;

            ExportPdfButton.IsEnabled = !running;
            AddCurrentButton.IsEnabled = !running;
            AddAllLcButton.IsEnabled = !running;

            // можно оставить остальное активным; либо при желании выключить ещё список/стрелки
        }


    }
}
