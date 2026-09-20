using Core.Librarys;
using Core.Librarys.Image;
using Core.Models;
using Core.Servicers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UI.Controls;
using UI.Controls.Charts.Model;
using UI.Models;
using UI.Servicers;
using UI.Views;
using static UI.Controls.SettingPanel.SettingPanel;

namespace UI.ViewModels
{
    public class DataPageVM : DataPageModel
    {
        public Command ToDetailCommand { get; set; }
        public Command SetCalendarMonthCommand { get; set; }
        public Command SetCalendarTodayCommand { get; set; }

        private readonly IData data;
        private readonly MainViewModel main;
        private readonly IAppContextMenuServicer appContextMenuServicer;
        private readonly IAppConfig appConfig;
        private readonly IWebData _webData;
        private readonly IWebSiteContextMenuServicer _webSiteContextMenu;

        public DataPageVM(IData data, MainViewModel main, IAppContextMenuServicer appContextMenuServicer, IAppConfig appConfig, IWebData webData, IWebSiteContextMenuServicer webSiteContextMenu)
        {
            this.data = data;
            this.main = main;
            this.appContextMenuServicer = appContextMenuServicer;
            this.appConfig = appConfig;
            _webData = webData;
            _webSiteContextMenu = webSiteContextMenu;

            ToDetailCommand = new Command(new Action<object>(OnTodetailCommand));
            SetCalendarMonthCommand = new Command(new Action<object>(OnSetCalendarMonthCommand));
            SetCalendarTodayCommand = new Command(new Action<object>(obj => SetCalendarToday()));

            Init();
        }

        public override void Dispose()
        {
            PropertyChanged -= DataPageVM_PropertyChanged;

            base.Dispose();
        }
        private void Init()
        {
            //LoadData(DateTime.Now.Date);
            PropertyChanged += DataPageVM_PropertyChanged;

            TabbarData = new System.Collections.ObjectModel.ObservableCollection<string>()
            {
                "按天","按月","按年"
            };

            TabbarSelectedIndex = 0;

            SetCalendarMonth(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));

            AppContextMenu = appContextMenuServicer.GetContextMenu();
        }

        private void OnTodetailCommand(object obj)
        {
            var data = obj as ChartsDataModel;

            if (data != null)
            {
                var model = data.Data as DailyLogModel;
                if (model != null && model.AppModel != null)
                {
                    main.Data = model.AppModel;
                    main.Uri = nameof(DetailPage);
                }
                else
                {
                    main.Data = data.Data as Core.Models.Db.WebSiteModel;
                    main.Uri = nameof(WebSiteDetailPage);
                }
            }
        }

        private void DataPageVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == nameof(DayDate))
            {
                LoadData(DayDate);
            }
            else if (e.PropertyName == nameof(MonthDate))
            {
                LoadData(MonthDate);
            }
            else if (e.PropertyName == nameof(YearDate))
            {
                LoadData(YearDate);
            }
            else if (e.PropertyName == nameof(TabbarSelectedIndex))
            {
                if (TabbarSelectedIndex == 0)
                {
                    if (DayDate == DateTime.MinValue)
                    {
                        DayDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    }

                }
                else if (TabbarSelectedIndex == 1)
                {
                    if (MonthDate == DateTime.MinValue)
                    {
                        MonthDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    }
                }
                else if (TabbarSelectedIndex == 2)
                {
                    if (YearDate == DateTime.MinValue)
                    {
                        YearDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    }
                }
            }
            else if (e.PropertyName == nameof(SelectedCalendarDay))
            {
                var day = SelectedCalendarDay;
                if (day == null || !day.CanSelect)
                {
                    return;
                }
                if (CalendarDays != null)
                {
                    foreach (var item in CalendarDays)
                    {
                        item.IsSelected = ReferenceEquals(item, day);
                    }
                }
                if (TabbarSelectedIndex == 0 && DayDate.Date != day.Date)
                {
                    DayDate = day.Date;
                }
            }
            else if (e.PropertyName == nameof(ShowType))
            {
                LoadData(DayDate, 0);
                LoadData(MonthDate, 1);
                LoadData(YearDate, 2);
                LoadCalendarDays();
                if (ShowType.Id == 0)
                {
                    AppContextMenu = appContextMenuServicer.GetContextMenu();
                }
                else
                {
                    AppContextMenu = _webSiteContextMenu.GetContextMenu();
                }
            }
        }



        #region 日历

        private DateTime calendarMonth;

        /// <summary>
        /// 切换日历显示月份
        /// </summary>
        private void SetCalendarMonth(DateTime month)
        {
            calendarMonth = new DateTime(month.Year, month.Month, 1);
            CalendarMonthStr = calendarMonth.ToString("yyyy年MM月");
            LoadCalendarDays();
        }

        private void OnSetCalendarMonthCommand(object obj)
        {
            int offset = int.Parse(obj.ToString());
            var newMonth = calendarMonth.AddMonths(offset);
            var now = DateTime.Now;
            if (newMonth > new DateTime(now.Year, now.Month, 1) || newMonth < new DateTime(2020, 1, 1))
            {
                return;
            }
            SetCalendarMonth(newMonth);
        }

        private void SetCalendarToday()
        {
            var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            if (calendarMonth.Year != now.Year || calendarMonth.Month != now.Month)
            {
                SetCalendarMonth(now);
            }
            if (TabbarSelectedIndex == 0 && DayDate.Date != now.Date)
            {
                DayDate = now;
            }
        }

        /// <summary>
        /// 加载日历月份数据（每日使用总时长）
        /// </summary>
        private async void LoadCalendarDays()
        {
            var month = calendarMonth;
            var isApp = ShowType != null && ShowType.Id == 0;
            List<CalendarDayModel> list = null;

            await Task.Run(() =>
            {
                var start = month;
                var end = month.AddMonths(1).AddDays(-1);

                double[] totals = null;
                if (isApp)
                {
                    totals = data.GetRangeTotalData(start, end);
                }
                double max = totals != null && totals.Length > 0 ? totals.Max() : 0;

                int days = DateTime.DaysInMonth(month.Year, month.Month);
                //周一为首列
                int prePad = ((int)start.DayOfWeek + 6) % 7;
                var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                list = new List<CalendarDayModel>();
                for (int i = prePad; i > 0; i--)
                {
                    list.Add(new CalendarDayModel()
                    {
                        Date = start.AddDays(-i),
                        IsOtherMonth = true,
                        CellOpacity = .3,
                    });
                }

                for (int i = 0; i < days; i++)
                {
                    var date = start.AddDays(i);
                    int total = totals != null && i < totals.Length ? (int)totals[i] : 0;
                    var isFuture = date > today;

                    list.Add(new CalendarDayModel()
                    {
                        Date = date,
                        IsToday = date == today,
                        IsFuture = isFuture,
                        CellOpacity = isFuture ? .3 : 1,
                        TimeText = isApp && total > 0 ? FormatDayTime(total) : "",
                        Heat = isApp && max > 0 && total > 0 ? Math.Min(.85, .15 + total / max * .7) : 0,
                        ToolTipText = date.ToString("yyyy年MM月dd日") + (isApp ? (total > 0 ? "\r\n使用时长：" + Time.ToString(total) : "\r\n无使用数据") : ""),
                    });
                }
            });

            CalendarDays = list;

            //还原选中日期（换月时保持同号数）
            var dayNum = DayDate != DateTime.MinValue ? DayDate.Day : DateTime.Now.Day;
            var selected = list.Where(m => m.CanSelect && m.DayNum == dayNum).FirstOrDefault();
            if (selected != null)
            {
                selected.IsSelected = true;
            }
            SelectedCalendarDay = selected;
        }

        /// <summary>
        /// 短格式时长
        /// </summary>
        private string FormatDayTime(int seconds)
        {
            if (seconds >= 3600)
            {
                return (seconds / 3600.0).ToString("0.0") + "小时";
            }
            if (seconds >= 60)
            {
                return (seconds / 60) + "分";
            }
            return seconds + "秒";
        }

        #endregion

        #region 读取数据




        private async void LoadData(DateTime date, int dataType_ = -1)
        {
            await Task.Run(() =>
            {
                DateTime dateStart = date, dateEnd = date;

                dataType_ = dataType_ == -1 ? TabbarSelectedIndex : dataType_;

                if (dataType_ == 1)
                {
                    dateStart = new DateTime(date.Year, date.Month, 1);
                    dateEnd = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                }
                else if (dataType_ == 0)
                {
                    dateStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                    dateEnd = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
                }
                else if (dataType_ == 2)
                {
                    dateStart = new DateTime(date.Year, 1, 1, 0, 0, 0);
                    dateEnd = new DateTime(date.Year, 12, DateTime.DaysInMonth(date.Year, 12), 23, 59, 59);
                }

                List<ChartsDataModel> chartData = new List<ChartsDataModel>();
                if (ShowType.Id == 0)
                {
                    var result = data.GetDateRangelogList(dateStart, dateEnd);
                    chartData = MapToChartsData(result);
                }
                else
                {
                    var result = _webData.GetWebSiteLogList(dateStart, dateEnd);
                    chartData = MapToChartsWebData(result);
                }


                if (dataType_ == 0)
                {
                    Data = chartData;
                }
                else if (dataType_ == 1)
                {
                    MonthData = chartData;
                }
                else
                {
                    YearData = chartData;
                }

            });
        }

        #region 处理数据
        private List<ChartsDataModel> MapToChartsData(IEnumerable<Core.Models.DailyLogModel> list)
        {
            var resData = new List<ChartsDataModel>();
            try
            {
                var config = appConfig.GetConfig();

                foreach (var item in list)
                {
                    var bindModel = new ChartsDataModel();
                    bindModel.Data = item;
                    bindModel.Name = !string.IsNullOrEmpty(item.AppModel?.Alias) ? item.AppModel.Alias : string.IsNullOrEmpty(item.AppModel?.Description) ? item.AppModel.Name : item.AppModel.Description;
                    bindModel.Value = item.Time;
                    bindModel.Tag = Time.ToString(item.Time);
                    bindModel.PopupText = item.AppModel?.File;
                    bindModel.Icon = item.AppModel?.IconFile;
                    bindModel.BadgeList = new List<ChartBadgeModel>();
                    if (item.AppModel.Category != null)
                    {
                        bindModel.BadgeList.Add(new ChartBadgeModel()
                        {
                            Name = item.AppModel.Category.Name,
                            Color = item.AppModel.Category.Color,
                            Type = ChartBadgeType.Category
                        });
                    }
                    if (config.Behavior.IgnoreProcessList.Contains(item.AppModel.Name))
                    {
                        bindModel.BadgeList.Add(ChartBadgeModel.IgnoreBadge);
                    }
                    resData.Add(bindModel);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            return resData;
        }

        private List<ChartsDataModel> MapToChartsWebData(IEnumerable<Core.Models.Db.WebSiteModel> list)
        {
            var resData = new List<ChartsDataModel>();
            try
            {
                var config = appConfig.GetConfig();

                foreach (var item in list)
                {
                    var bindModel = new ChartsDataModel();
                    bindModel.Data = item;
                    bindModel.Name = !string.IsNullOrEmpty(item.Alias) ? item.Alias : item.Title;
                    bindModel.Value = item.Duration;
                    bindModel.Tag = Time.ToString(item.Duration);
                    bindModel.PopupText = item.Domain;
                    bindModel.Icon = item.IconFile;
                    bindModel.BadgeList = new List<ChartBadgeModel>();
                    if (item.Category != null)
                    {
                        bindModel.BadgeList.Add(new ChartBadgeModel()
                        {
                            Name = item.Category.Name,
                            Color = item.Category.Color,
                            Type = ChartBadgeType.Category
                        });
                    }
                    if (config.Behavior.IgnoreURLList.Contains(item.Domain))
                    {
                        bindModel.BadgeList.Add(ChartBadgeModel.IgnoreBadge);
                    }
                    resData.Add(bindModel);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            return resData;
        }
        #endregion
        #endregion
    }
}
