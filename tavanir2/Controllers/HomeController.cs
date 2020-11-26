using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tavanir2.Models;

namespace tavanir2.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBaseRepository baseRepository;

        public HomeController(IBaseRepository baseRepository)
        {
            this.baseRepository = baseRepository;
        }

        // ذخیره فایل در سرور:
        private async Task<string> UploadDbFileAndRetPath(UploadViewModel model)
        {
            string dir_path = Path.Combine(Directory.GetCurrentDirectory(), "ExcelFiles");
            string path = Path.Combine(
                  dir_path,
                  string.Concat(DateTime.Now.Ticks.ToString(), "_", model.File.FileName));

            if (!Directory.Exists(dir_path))
            {
                Directory.CreateDirectory(dir_path);
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            return path;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!baseRepository.ValidationToken())
            {
                return Redirect("/Login/Index");
            }
            SetViewBag();

            UploadViewModel model = new UploadViewModel()
            {
                ListDataCategories = GetDataCategories(),
                SheetName = "Sheet1"
            };
            return View(model);
        }

        private List<DataCategories> GetDataCategories()
        {
            List<DataCategories> dataCategories = baseRepository.ExecuteCommand(conn =>
                 conn.Query<DataCategories>(@"SELECT [Id], [Name]" +
                 @" FROM [TavanirStage].[Basic].[DataCategories]" +
                 @" WHERE [Enabled] = '1'").ToList());

            return dataCategories;
        }

        private void SetViewBag()
        {
            ViewBag.DashUrl = HttpContext.Session.GetString("DashUrl");
            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");
            ViewBag.Code = HttpContext.Session.GetString("Code");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index([FromForm] UploadViewModel model)
        {
            if (!baseRepository.ValidationToken())
            {
                return Redirect("/Login/Index");
            }

            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Code")))
            {
                return Redirect("/Home/Report");
            }

            SetViewBag();

            model.ColumnsType = "Name";

            if (!ModelState.IsValid)
            {
                model.ListDataCategories = GetDataCategories();
                return View(model);
            }

            string file_path = null;
            try
            {
                string file_type = model.File.FileName.Substring(model.File.FileName.LastIndexOf("."));
                if (!(Equals(file_type, ".xlsx") || Equals(file_type, ".xls")))
                {
                    ModelState.AddModelError(nameof(model.File), "نوع فایل فقط می‌تواند xlx و یا xlsx باشد.");
                    model.ListDataCategories = GetDataCategories();
                    return View(model);
                }

                file_path = UploadDbFileAndRetPath(model).Result;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "خطا در آپلود فایل: " + ex.Message.ToString());
            }

            try
            {
                string msg = UsingOleDb(file_path, model);
                if (string.IsNullOrEmpty(msg))
                {
                    return Redirect("/Home/Report/saved=1");
                }

                ModelState.AddModelError(string.Empty, msg);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "خطا در ثبت اطلاعات: " + ex.Message.ToString());
            }

            model.ListDataCategories = GetDataCategories();
            return View(model);
        }

        private string UsingOleDb(string inFilePath, UploadViewModel model)
        {
            string companyId = HttpContext.Session.GetString("CompanyId");

            List<DataMembers> dataMembers = baseRepository.ExecuteCommand(conn =>
                 conn.Query<DataMembers>(@"SELECT [DI].[" + model.ColumnsType + @"] AS [ColumnValue]" +
                 @", [DM].[Id], [DM].[DataSetId], [DM].[DataItemId], [DM].[RegularExperssion], [DM].[Description]" +
                 @" FROM [TavanirStage].[Stage].[DataMembers] AS [DM]" +
                 @" INNER JOIN [TavanirStage].[Basic].[DataItems] AS [DI] ON [DM].[DataItemId] = [DI].[Id]" +
                 @" AND [DI].[Enabled] = '1'" +
                 @" WHERE [DM].[Enabled] = '1' AND [DI].[DataCategoryID] = @DataCategoryID",
                 new { @DataCategoryID = model.DataCategoryID }).ToList());

            if ((dataMembers?.Any() ?? false) == false)
            {
                return "فیلد فعالی موجود نمی‌باشد.";
            }

            List<TimeSeries> timeSeries = null;
            List<HistoricalValues> historicalValues = null;

            PersianCalendar pc = new PersianCalendar();
            int default_year = pc.GetYear(DateTime.Now);

            int year; byte? month; byte? dayOfMonth; string timeOfDay = string.Empty;

            int i = 0;
            DataMembers item = null;
            string recivedValue = null;

            DateTime receiption = DateTime.Now;

            //"HDR=Yes;" indicates that the first row contains column names, not data.
            var connectionString = $@"
            Provider=Microsoft.ACE.OLEDB.12.0;
            Data Source={inFilePath};
            Extended Properties=""Excel 12.0 Xml;HDR=YES""";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $@"SELECT * FROM [{model.SheetName}$]";
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.FieldCount == 0)
                    {
                        return string.Format("مقدار ای در فایل اکسل یافت نشد.");
                    }

                    for (int j = 0; j < dr.FieldCount; j++)
                    {
                        switch (dr.GetName(j))
                        {
                            case "Year": break;
                            case "Month": break;
                            case "DayOfMonth": break;
                            case "TimeOfDay": break;
                            default:
                                item = dataMembers.Where(c => c.ColumnValue == dr.GetName(j))?.FirstOrDefault();

                                if (item == null || item.DataSetId == null)
                                {
                                    return $"ستون «{dr.GetName(j)}» تعریف نشده و یا فعال نمی‌باشد.";
                                }
                                break;
                        }
                    }


                    timeSeries = new List<TimeSeries>();
                    historicalValues = new List<HistoricalValues>(dr.FieldCount);
                    while (dr.Read())
                    {
                        i++;

                        year = default_year;
                        month = null;
                        dayOfMonth = null;
                        timeOfDay = "";
                        for (int j = 0; j < dr.FieldCount; j++)
                        {
                            string val = dr.GetValue(j).ToString();
                            switch (dr.GetName(j))
                            {
                                case "Year":
                                    if (!int.TryParse(val, out year))
                                    {
                                        year = default_year;
                                    }
                                    break;
                                case "Month":
                                    if (!string.IsNullOrEmpty(val))
                                    {
                                        if (!byte.TryParse(val, out byte month2))
                                        {
                                            return $"مقدار ماه وارد شده ({val}) در سطر {i} معتبر نمی‌باشد.";
                                        }
                                        if (!(month2 >= 1 && month2 <= 12))
                                        {
                                            return $"مقدار ماه وارد شده ({month2}) در سطر {i} در بازه مجاز نمی‌باشد!";
                                        }
                                        month = month2;
                                    }
                                    break;
                                case "DayOfMonth":

                                    if (!string.IsNullOrEmpty(val))
                                    {
                                        if (!byte.TryParse(val, out byte dayOfMonth2))
                                        {
                                            return $"مقدار روز ({val}) از ماه وارد شده در سطر {i} معتبر نمی‌باشد.";
                                        }

                                        dayOfMonth = dayOfMonth2;
                                    }
                                    break;
                                case "TimeOfDay":
                                    if (!string.IsNullOrEmpty(val))
                                    {
                                        if (val.IndexOf(":") == -1 || !DateTime.TryParse(val, out DateTime timeOfDay2))
                                        {
                                            return $"مقدار زمان ({val}) وارد شده در سطر {i} معتبر نمی‌باشد.";
                                        }
                                        timeOfDay = timeOfDay2.ToString("HH:mm:ss");
                                    }
                                    break;
                                default:
                                    recivedValue = val;
                                    item = dataMembers.Where(c => c.ColumnValue == dr.GetName(j)).First();

                                    if (!string.IsNullOrEmpty(item.RegularExperssion))
                                    {
                                        Regex rgx = new Regex(item.RegularExperssion);
                                        if (!rgx.IsMatch(recivedValue))
                                        {
                                            return string.Concat("مقدار «", recivedValue,
                                                " برای ستون «", item.ColumnValue,
                                                "» (به شماره ستون ", j + 1,
                                                ") در سطر ", i,
                                                " معتبر نمی‌باشد. : ", item.Description);
                                        }
                                    }
                                    break;
                            }
                        }

                        if (!(year >= 1300 && year <= 9999))
                        {
                            return $"سال وارد شده ({year}) در سطر {i} در بازه مجاز نمی‌باشد.";
                        }
                        if (dayOfMonth.HasValue)
                        {
                            if (month.HasValue)
                            {
                                return $"برای سطر {i} مقدار روز ({dayOfMonth.Value}) وارد شده درصورتی که مقدار ماه ای تعریف نشده است.";
                            }
                            if (!((month.Value < 7 && (dayOfMonth.Value >= 1 && dayOfMonth.Value <= 31)) || (month.Value > 6 && (dayOfMonth.Value >= 1 && dayOfMonth.Value <= 30))))
                            {
                                return $"روز وارد شده ({dayOfMonth.Value}) برای ماه {month.Value} در سطر {i} در بازه مجاز نمی‌باشد.";
                            }
                        }


                        string timeSeries_Id = timeSeries.Where(c => c.Year == year && c.Month == month && c.DayOfMonth == dayOfMonth && c.TimeOfDay == timeOfDay)
                            .Select(c => c.Id).FirstOrDefault();
                        if (string.IsNullOrEmpty(timeSeries_Id))
                        {
                            timeSeries_Id = Guid.NewGuid().ToString();
                            timeSeries.Add(new TimeSeries()
                            {
                                Id = timeSeries_Id,
                                Year = year,
                                TimeOfDay = timeOfDay,
                                Enabled = "1"
                            });
                            if (month.HasValue)
                            {
                                timeSeries[timeSeries.Count - 1].Month = month.Value;
                            }
                            if (dayOfMonth.HasValue)
                            {
                                timeSeries[timeSeries.Count - 1].DayOfMonth = dayOfMonth.Value;
                            }
                        }

                        historicalValues.Add(new HistoricalValues()
                        {
                            Id = Guid.NewGuid(),
                            TimeSeriesId = timeSeries_Id,
                            RowIndex = i,
                            Receiption = receiption,
                            RecivedValue = recivedValue,
                            DataMemberId = item.Id,
                            Approved = "1"
                        });

                    }
                }
            }

            if (i == 0)
            {
                return "سطری یافت نشد.";
            }

            Guid token = Guid.NewGuid();

            StringBuilder queryBuilder_TimeSeries = new StringBuilder(2048);
            queryBuilder_TimeSeries.Append("INSERT INTO [TavanirStage].[Basic].[TimeSeries]([Id],[Token],[Year],[Month],[DayOfMonth],[TimeOfDay],[Enabled])VALUES");
            for (int j = 0; j < timeSeries.Count; j++)
            {
                if (j > 0)
                {
                    queryBuilder_TimeSeries.Append(",");
                }
                queryBuilder_TimeSeries.Append("('");
                queryBuilder_TimeSeries.Append(timeSeries[j].Id.ToString());
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(token.ToString());
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(timeSeries[j].Year);
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(timeSeries[j].Month.HasValue ? timeSeries[j].Month.Value.ToString() : "");
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(timeSeries[j].DayOfMonth.HasValue ? timeSeries[j].DayOfMonth.Value.ToString() : "");
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(timeSeries[j].TimeOfDay);
                queryBuilder_TimeSeries.Append("','");
                queryBuilder_TimeSeries.Append(timeSeries[j].Enabled);
                queryBuilder_TimeSeries.Append("')");
            }

            StringBuilder queryBuilder_HistoricalValues = new StringBuilder(4096);
            queryBuilder_HistoricalValues.Append("INSERT [TavanirStage].[Stage].[HistoricalValues]([Id],[TimeSeriesId],[RowIndex],[RecivedValue],[Approved],[Receiption],[DataMemberId])VALUES");
            for (int j = 0; j < historicalValues.Count; j++)
            {
                if (j > 0)
                {
                    queryBuilder_HistoricalValues.Append(",");
                }
                queryBuilder_HistoricalValues.Append("('");
                queryBuilder_HistoricalValues.Append(historicalValues[j].Id.ToString());
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].TimeSeriesId.ToString());
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].RowIndex);
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].RecivedValue);
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].Approved);
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].Receiption);
                queryBuilder_HistoricalValues.Append("','");
                queryBuilder_HistoricalValues.Append(historicalValues[j].DataMemberId);
                queryBuilder_HistoricalValues.Append("')");
            }


            bool is_valid_code = false;
            string authorizationTokens_Code = null;
            while (is_valid_code == false)
            {
                authorizationTokens_Code = RandomString(10);

                string ck_code = baseRepository.ExecuteCommand(conn =>
                    conn.Query<string>("SELECT [Code] FROM [TavanirStage].[Stage].[AuthorizationTokens] WHERE [Code] = @Code",
                        new { @Code = authorizationTokens_Code }).FirstOrDefault());
                if (string.IsNullOrEmpty(ck_code))
                {
                    is_valid_code = true;
                }
            }

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query("INSERT INTO [TavanirStage].[Stage].[AuthorizationTokens] ([Token], [CreatedDate], [CompanyId], [Code]) VALUES (@Token, @CreatedDate, @CompanyId, @Code)",
                    new { @Token = token, @CompanyId = companyId, @CreatedDate = receiption, @Code = authorizationTokens_Code });
            });

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query(queryBuilder_TimeSeries.ToString());
            });

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query(queryBuilder_HistoricalValues.ToString());
            });

            HttpContext.Session.SetString("Code", authorizationTokens_Code);

            return string.Empty;
        }

        private string RandomString(int size, bool lowerCase = true)
        {
            Random random = new Random();
            var builder = new StringBuilder(size);

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length=26  

            for (var i = 0; i < size; i++)
            {
                var @char = (char)random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        [HttpGet]
        public IActionResult Report()
        {
            if (!baseRepository.ValidationToken())
            {
                return Redirect("/Login/Index");
            }

            SetViewBag();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
