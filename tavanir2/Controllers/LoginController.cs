using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using tavanir2.Models;

namespace tavanir2.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly IBaseRepository baseRepository;
        private readonly HashingPassword hashingPassword;

        public LoginController(IConfiguration configuration, IBaseRepository baseRepository, HashingPassword hashingPassword)
        {
            this.configuration = configuration;
            this.baseRepository = baseRepository;
            this.hashingPassword = hashingPassword;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logout()
        {
            if (HttpContext.Session.HasKey("CompanyId"))
                HttpContext.Session.Remove("CompanyId");
            if (HttpContext.Session.HasKey("CompanyCode"))
                HttpContext.Session.Remove("CompanyCode");
            if (HttpContext.Session.HasKey("CompanyName"))
                HttpContext.Session.Remove("CompanyName");
            if (HttpContext.Session.HasKey("DashUrl"))
                HttpContext.Session.Remove("DashUrl");
            if (HttpContext.Session.HasKey("Code"))
                HttpContext.Session.Remove("Code");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "رمز عبور را وارد نمایید.");
                return View(model);
            }

            var res = baseRepository.ExecuteCommand(conn =>
                conn.Query<Company>("SELECT [Id], [Code], [Name], [Enabled], [Password], [PasswordHash], [PasswordSalt] FROM [TavanirStage].[Basic].[Companies] WHERE [Username] = @Username",
                    new { model.Username }).FirstOrDefault());

            if (res == null || res.Id == null || Equals(res.Id, Guid.Empty))
            {
                ModelState.AddModelError(nameof(model.Username), "نام کاربری یافت نشد.");
                return View(model);
            }

            if (!Equals(res.Password, model.Password) && !hashingPassword.VerifyPassword(res.PasswordHash, res.PasswordSalt, model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "رمز عبور صحیح نیست.");
                return View(model);
            }

            if (!res.Enabled)
            {
                ModelState.AddModelError(string.Empty, "حساب کاربری شما فعال نمی‌باشد.");
                return View(model);
            }


            string companyId = res.Id.ToString();
            HttpContext.Session.SetString("CompanyId", companyId);
            HttpContext.Session.SetString("CompanyCode", res.Code);
            HttpContext.Session.SetString("CompanyName", res.Name);
            HttpContext.Session.SetString("DashUrl", string.Concat(configuration.GetSection("DashboardAddress").Value, companyId));

            if (HttpContext.Session.HasKey("Code"))
                HttpContext.Session.Remove("Code");

            return Redirect("/Home/Index");
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = GetRegisterAccountModel();
            return View(model);
        }

        private RegisterAccountViewModel GetRegisterAccountModel()
        {
            List<Locations> list = baseRepository.ExecuteCommand(conn =>
                 conn.Query<Locations>("SELECT [loc1].[LocationId]," +
                 " CONCAT((CASE WHEN [loc4].[NAME] IS NULL THEN '' ELSE CONCAT([loc4].[NAME], N' : ') END)," +
                 " (CASE WHEN [loc3].[NAME] IS NULL THEN '' ELSE CONCAT([loc3].[NAME], N' : ') END)," +
                 " (CASE WHEN [loc2].[NAME] IS NULL THEN N'استان ' ELSE CONCAT([loc2].[NAME], N' : ') END), [loc1].[NAME]) AS [NAME]" +
                 " FROM [TavanirStage].[Basic].[Locations] AS [loc1]" +
                 " LEFT JOIN [TavanirStage].[Basic].[Locations] AS [loc2] ON [loc1].[ParentId] = [loc2].[LocationId]" +
                 " LEFT JOIN [TavanirStage].[Basic].[Locations] AS [loc3] ON [loc2].[ParentId] = [loc3].[LocationId]" +
                 " LEFT JOIN [TavanirStage].[Basic].[Locations] AS [loc4] ON [loc3].[ParentId] = [loc4].[LocationId]" +
                 " WHERE [loc1].[Enabled] = '1' AND ISNULL([loc2].[Enabled], '1') = '1' AND ISNULL([loc3].[Enabled], '1') = '1' AND ISNULL([loc4].[Enabled], '1') = '1'" +
                 " ORDER BY [NAME]").ToList());

            RegisterAccountViewModel model = new RegisterAccountViewModel()
            {
                Locations = list
            };
            return model;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var res = baseRepository.ExecuteCommand(conn =>
                conn.Query<Company>("SELECT [Id] FROM [TavanirStage].[Basic].[Companies] WHERE [Code] = @Code",
                    new { model.Code }).ToList());
            if (res?.Any() ?? false)
            {
                ModelState.AddModelError(model.Code, "کد شرکت تکراری است.");
                return View(model);
            }

            res = baseRepository.ExecuteCommand(conn =>
                conn.Query<Company>("SELECT [Id] FROM [TavanirStage].[Basic].[Companies] WHERE [Name] = @Name",
                    new { model.Name }).ToList());
            if (res?.Any() ?? false)
            {
                ModelState.AddModelError(model.Username, "نام شرکت تکراری است.");
                return View(model);
            }

            res = baseRepository.ExecuteCommand(conn =>
                conn.Query<Company>("SELECT [Id] FROM [TavanirStage].[Basic].[Companies] WHERE [Username] = @Username",
                    new { model.Username }).ToList());
            if (res?.Any() ?? false)
            {
                ModelState.AddModelError(model.Username, "نام کاربری تکراری است.");
                return View(model);
            }

            var passHashed = hashingPassword.HashPassword(model.Password);

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query("INSERT INTO [TavanirStage].[Basic].[Companies] ([Id], [Code], [Name], [LocationId], [Description], [Username], [Password], [PasswordHash], [PasswordSalt]) VALUES (NEWID(), @Code, @Name, @LocationId, @Description, @Username, @Password, @PasswordHash, @PasswordSalt)",
                    new { model.Code, model.Name, model.LocationId, model.Description, model.Username, model.Password, passHashed.PasswordHash, passHashed.PasswordSalt });
            });

            return Redirect("/Login/Register");
        }
    }
}
