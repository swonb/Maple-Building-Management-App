﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Maple_Building_Management_App.Models;
using DataLibrary;
using static DataLibrary.Logic.AccountProcessor;

namespace Maple_Building_Management_App.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult ViewAccounts()
        {
            ViewBag.Message = "Accounts List";

            var data = LoadAccounts();

            List<AccountModel> accounts = new List<AccountModel>();

            foreach (var row in data)
            {
                accounts.Add(new AccountModel
                {
                    FirstName = row.FirstName,
                    LastName = row.LastName,
                    EmailAddress = row.EmailAddress,
                    Tenant = row.Tenant,
                    PropertyCode = row.PropertyCode
                });
            }
            return View(accounts);
        }

        public ActionResult Register()
        {
            ViewBag.Message = "App Registration";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(AccountModel model)
        {
            if (ModelState.IsValid)
            {
                int recordsCreated = CreateAccount(
                    model.FirstName, 
                    model.LastName, 
                    model.EmailAddress, 
                    model.Tenant, 
                    model.PropertyCode);
                return RedirectToAction("Index");
            }

            return View();
        }

        public ActionResult CreateComplaint()
        {
            ViewBag.Message = "Create Complaint";


            List<SelectListItem> items = new List<SelectListItem>();

            items.Add(new SelectListItem { Text = "Action", Value = "0" });

            items.Add(new SelectListItem { Text = "Drama", Value = "1" });

            items.Add(new SelectListItem { Text = "Comedy", Value = "2", Selected = true });

            items.Add(new SelectListItem { Text = "Science Fiction", Value = "3" });

            ViewBag.ComplaintTypes = items;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateComplaint(ComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                //int recordsCreated = CreateComplaint(
                    //model.FirstName,
                    //model.LastName,
                    //model.EmailAddress,
                    //model.Tenant,
                    //model.PropertyCode
                //    );
                return RedirectToAction("Index");
            }

            return View();
        }
    }
}