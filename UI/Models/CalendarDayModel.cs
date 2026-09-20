using System;
using System.Windows;
using System.Windows.Media;

namespace UI.Models
{
    /// <summary>
    /// 日历单元格数据
    /// </summary>
    public class CalendarDayModel : UINotifyPropertyChanged
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 日
        /// </summary>
        public int DayNum
        {
            get { return Date.Day; }
        }

        /// <summary>
        /// 当天使用时长（短格式）
        /// </summary>
        public string TimeText { get; set; }

        /// <summary>
        /// 悬浮提示
        /// </summary>
        public string ToolTipText { get; set; }

        /// <summary>
        /// 热力背景透明度
        /// </summary>
        public double Heat { get; set; }

        /// <summary>
        /// 单元格透明度（补白/未来日期置灰）
        /// </summary>
        public double CellOpacity { get; set; } = 1;

        /// <summary>
        /// 是否上月补白日期
        /// </summary>
        public bool IsOtherMonth { get; set; }

        /// <summary>
        /// 是否未来日期
        /// </summary>
        public bool IsFuture { get; set; }

        /// <summary>
        /// 是否可选择
        /// </summary>
        public bool CanSelect
        {
            get { return !IsOtherMonth && !IsFuture; }
        }

        private bool IsToday_;
        /// <summary>
        /// 是否今天
        /// </summary>
        public bool IsToday
        {
            get { return IsToday_; }
            set { IsToday_ = value; OnPropertyChanged(); OnPropertyChanged(nameof(DayWeight)); }
        }

        /// <summary>
        /// 日期数字加粗（今天）
        /// </summary>
        public FontWeight DayWeight
        {
            get { return IsToday ? FontWeights.Bold : FontWeights.Normal; }
        }

        private bool IsSelected_;
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get { return IsSelected_; }
            set { IsSelected_ = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedBorderVisibility)); }
        }

        /// <summary>
        /// 选中边框显示
        /// </summary>
        public Visibility SelectedBorderVisibility
        {
            get { return IsSelected ? Visibility.Visible : Visibility.Collapsed; }
        }
    }
}
