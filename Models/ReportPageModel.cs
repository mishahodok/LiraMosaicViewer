namespace LiraMosaicViewer.Models
{
    public sealed class ReportPageModel
    {
        // 3 строки сверху слева
        public string LoadTitle { get; set; } = "Снег";
        public string MosaicTitle { get; set; } = "Мозаика напряжений по My";
        public string UnitsTitle { get; set; } = "Единицы измерения - т/м2";

        // Подпись под рисунком
        public string FigureCaption { get; set; } = "Рис. 1 ...";

        // Номер листа (в штампе)
        public string SheetNumber { get; set; } = "1";

        // Если нет данных — показываем текст в области рисунка
        public string NoDataText { get; set; } = "";

        public ReportPageModel Clone() => new ReportPageModel
        {
            LoadTitle = this.LoadTitle,
            MosaicTitle = this.MosaicTitle,
            UnitsTitle = this.UnitsTitle,
            FigureCaption = this.FigureCaption,
            SheetNumber = this.SheetNumber,
            NoDataText = this.NoDataText
        };
    }
}
