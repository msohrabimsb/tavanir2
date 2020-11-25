﻿using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using tavanir2.Models;

namespace tavanir2.Controllers
{
    public class LoginController : Controller
    {
        private readonly IBaseRepository baseRepository;
        private readonly HashingPassword hashingPassword;

        public LoginController(IBaseRepository baseRepository, HashingPassword hashingPassword)
        {
            this.baseRepository = baseRepository;
            this.hashingPassword = hashingPassword;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var res = baseRepository.ExecuteCommand(conn =>
                conn.Query<Company>("SELECT [Id], [Code], [Name], [Enabled], [Password], [PasswordHash], [PasswordSalt] FROM [TavanirStage].[Basic].[Companies] WHERE [Username] = @Username",
                    new { model.Username }).FirstOrDefault());

            if (res == null || res.Id == null || string.IsNullOrEmpty(res.Id.ToString()))
            {
                ModelState.AddModelError(nameof(model.Username), "نام کاربری یافت نشد.");
                return View(model);
            }

            if (!Equals(res.Password, model.Password) || !hashingPassword.VerifyPassword(res.PasswordHash, res.PasswordSalt, model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "رمز عبور صحیح نیست.");
                return View(model);
            }

            if (!res.Enabled)
            {
                ModelState.AddModelError(string.Empty, "حساب کاربری شما غیر فعال است.");
                return View(model);
            }

            string loginToken = Guid.NewGuid().ToString();

            baseRepository.ExecuteCommand(conn =>
            {
                var query = conn.Query("INSERT INTO  [TavanirStage].[Stage].[AuthorizationTokens] ([Token], [CreatedDate], [CompanyId], [Code]) VALUES (@Token, GETDATE(), @CompanyId, @Code)",
                    new { @Token = loginToken, @CompanyId = res.Id, @Code = res.Code });
            });

            HttpContext.Session.SetString("CompanyId", res.Id.ToString());
            HttpContext.Session.SetString("CompanyName", res.Name);
            HttpContext.Session.SetString("LoginToken", loginToken);

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
                 conn.Query<Locations>("SELECT [LocationId], [Name] FROM [TavanirStage].[Basic].[Locations] WHERE [Enabled] = '1'").ToList());

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
