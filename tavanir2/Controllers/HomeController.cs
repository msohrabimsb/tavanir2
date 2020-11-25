using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
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
                  model.File.FileName);

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
            ViewBag.LoginToken = HttpContext.Session.GetString("LoginToken");
            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index([FromForm] UploadViewModel model)
        {
            if (!baseRepository.ValidationToken())
            {
                return Redirect("/Login/Index");
            }

            ViewBag.LoginToken = HttpContext.Session.GetString("LoginToken");
            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");

            if (!ModelState.IsValid)
                return View(model);

            string file_path = null;
            try
            {
                string file_type = model.File.FileName.Substring(model.File.FileName.LastIndexOf("."));
                if (!(Equals(file_type, ".xlsx") || Equals(file_type, ".xls")))
                {
                    ModelState.AddModelError(nameof(model.File), "نوع فایل فقط می‌تواند xlx و یا xlsx باشد.");
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

            return View(model);
        }

        private string UsingOleDb(string inFilePath, UploadViewModel model)
        {
            string token = HttpContext.Session.GetString("LoginToken");

            List<DataItemsInSets> dataItemsInSets = baseRepository.ExecuteCommand(conn =>
                 conn.Query<DataItemsInSets>(@"SELECT [DI].[" + model.ColumnsType + @"] AS [ColumnValue]" +
                 @", [DIIS].[DataSetId], [DIIS].[DataItemId], [DIIS].[RegularExperssion], [DIIS].[ValidationRule]" +
                 @", [DIIS].[ValidationMessage]" +
                 @" FROM [TavanirStage].[Stage].[DataItemsInSets] AS [DIIS]" +
                 @" INNER JOIN [TavanirStage].[Basic].[DataItems] AS [DI] ON [DIIS].[DataItemId] = [DI].[Id]" +
                 @" AND [DI].[Enabled] = '1'" +
                 @" WHERE [DIIS].[Enabled] = '1'").ToList());

            if ((dataItemsInSets?.Any() ?? false) == false)
            {
                return "فیلد فعالی موجود نمی‌باشد.";
            }

            int i = 0;
            StringBuilder queryBuilder = new StringBuilder(4096);

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
                    if (dr.FieldCount != dataItemsInSets.Count)
                    {
                        return string.Format("تعداد ستوی های فایل اکسل ({0} ستون) با تعداد ستون های تعیین شده فعال ({1} ستون) مطابقت ندارد.", dr.FieldCount, dataItemsInSets.Count);
                    }
                    for (int j = 0; j < dataItemsInSets.Count; j++)
                    {
                        DataItemsInSets item = dataItemsInSets.Where(c => c.ColumnValue == dr.GetName(j))?.FirstOrDefault();

                        if (item == null || item.DataSetId == null)
                        {
                            return $"ستون «{dr.GetName(j)}» تعریف نشده و یا فعال نمی‌باشد.";
                        }
                    }

                    queryBuilder.Append("INSERT INTO [TavanirStage].[Stage].[DataBatches]([Token],[DataSetId],[DataItemId],[RowIndex],[Value],[Approved],[Mesaage],[ReciveDateTime])VALUES");

                    while (dr.Read())
                    {
                        i++;
                        if (i > 1)
                        {
                            queryBuilder.Append(",");
                        }

                        for (int j = 0; j < dataItemsInSets.Count; j++)
                        {
                            if (j > 0)
                            {
                                queryBuilder.Append(",");
                            }

                            DataItemsInSets item = dataItemsInSets.Where(c => c.ColumnValue == dr.GetName(j)).First();

                            queryBuilder.Append("('");
                            queryBuilder.Append(token);
                            queryBuilder.Append("','");
                            queryBuilder.Append(item.DataSetId.ToString());
                            queryBuilder.Append("','");
                            queryBuilder.Append(item.DataItemId.ToString());
                            queryBuilder.Append("','");
                            queryBuilder.Append(i);
                            queryBuilder.Append("',N'");
                            queryBuilder.Append(dr.GetValue(j).ToString());
                            queryBuilder.Append("','");

                            if (!string.IsNullOrEmpty(item.RegularExperssion))
                            {
                                Regex rgx = new Regex(item.RegularExperssion);
                                if (!rgx.IsMatch(dr.GetValue(j).ToString()))
                                {
                                    string msg2 = string.Format("مقدار «{0}» برای ستون «{1}» (به شماره ستون {2}) در سطر {3} معتبر نمی‌باشد.",
                                            dr.GetValue(j).ToString(), item.ColumnValue, j + 1, i);
                                    if (!string.IsNullOrEmpty(item.ValidationMessage))
                                    {
                                        return msg2 + " / " + item.ValidationMessage;
                                    }
                                    else
                                    {
                                        return msg2;
                                    }
                                }
                            }

                            //if (!string.IsNullOrEmpty(item.ValidationRule))
                            //{
                            //    var arrValidationRule = item.ValidationRule.Split(';');

                            //    for (int k = 0; k < arrValidationRule.Length; k++)
                            //    {
                            //        var arrValidationItem = arrValidationRule[k].Split(',');

                            //        for (int l = 0; l < arrValidationItem.Length; l++)
                            //        {
                            //            var arrCheck = arrValidationItem[l].Split(':');
                            //            var itemCk = dataItemsInSets.Where(c => c.ColumnValue == arrCheck[0])?.FirstOrDefault();
                            //            if (itemCk != null && itemCk.DataSetId != null)
                            //            {

                            //            }
                            //        }
                            //    }
                            //}

                            // Approved:
                            queryBuilder.Append("1");

                            queryBuilder.Append("','");

                            // Mesaage:
                            queryBuilder.Append("");

                            queryBuilder.Append("',GETDATE())");
                        }
                    }
                }
            }

            if (i == 0)
            {
                return "سطری یافت نشد.";
            }

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query(queryBuilder.ToString());
            });

            return string.Empty;
        }

        [HttpGet]
        public IActionResult Report()
        {
            if (!baseRepository.ValidationToken())
            {
                return Redirect("/Login/Index");
            }

            ViewBag.LoginToken = HttpContext.Session.GetString("LoginToken");
            ViewBag.CompanyName = HttpContext.Session.GetString("CompanyName");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
