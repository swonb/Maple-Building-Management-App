﻿using Maple_Building_Management_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataLibrary.Models;
using AccountModel = DataLibrary.Models.AccountModel;
using System.Threading.Tasks;
using System.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.TwiML;
using Twilio.AspNet.Mvc;
using static DataLibrary.Logic.AccountProcessor;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace Maple_Building_Management_App.Controllers
{
    public class ManageController : TwilioController
    {
        // GET: Manage
        public ActionResult Index()
        {
            if (TempData.ContainsKey("Error"))
            {
                ViewBag.ErrorMessage = TempData["Error"];
            }

            ManageModel manageModel = new ManageModel();
            AccountModel dbModel = (AccountModel)Session["User"];
            manageModel.PhoneNumber = dbModel.PhoneNumber;
            manageModel.TwoFactor = dbModel.TwoFactor;
            return View(manageModel);
        }

        public ActionResult AddPhoneNumber()
        {
            ManageModel manageModel = new ManageModel();
            AccountModel dbModel = (AccountModel)Session["User"];
            manageModel.PhoneNumber = dbModel.PhoneNumber;
            return View(manageModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPhoneNumber(ManageModel model)
        {
            if (ModelState.IsValid)
            {
                model.Id = ((AccountModel)Session["User"]).Id; 
                ((AccountModel)Session["User"]).PhoneNumber = model.PhoneNumber;
                Session["Phone"] = model.PhoneNumber;
                return RedirectToAction("VerifyPhoneNumber");
            }
            return View();
        }

        public ActionResult VerifyPhoneNumber()
        {
            if (Session["Phone"] == null)
            {
                TempData["Error"] = "There is an error with your phone number. Please try adding again!";
                return RedirectToAction("Index");
            }
            if (Session["SentMessage"] == null || Session["ExpectedCode"] == null)
            {
                var accountSid = ConfigurationManager.AppSettings["SMSAccountIdentification"];
                var authToken = ConfigurationManager.AppSettings["SMSAccountPassword"];
                TwilioClient.Init(accountSid, authToken);

                var to = new PhoneNumber(Session["Phone"].ToString());
                var from = new PhoneNumber(ConfigurationManager.AppSettings["SMSAccountFrom"]);

                Random generator = new Random();
                string r = generator.Next(0, 999999).ToString("D6");
                Session["ExpectedCode"] = r;

                var message = MessageResource.Create(
                    to: to,
                    from: from,
                    body: "Your verification code for the Maple Building Management App is " + r);

                Session["SentMessage"] = message.Sid;
            }
            return View();

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyPhoneNumber(ManageModel model)
        {
            if (ModelState.IsValid)
            {
                if (Session["SentMessage"] == null || Session["ExpectedCode"] == null || Session["Phone"] == null)
                {
                    TempData["Error"] = "Message has timed out. Please resend a new request to add phone number!";
                    return RedirectToAction("Login");
                }
                else if (string.Equals(model.VerificationCode, Session["ExpectedCode"].ToString()))
                {
                    model.Id = ((AccountModel)Session["User"]).Id;
                    model.PhoneNumber = Session["Phone"].ToString();
                    int recordsUpdated = UpdatePhoneNumber(model.Id, model.PhoneNumber);
                    return RedirectToAction("SuccessPhoneNumber");
                }
                else
                {
                    ViewBag.ErrorMessage = "Please input the correct verification code!";
                }
            }
            return View();
        }

        public ActionResult RemovePhoneNumber()
        {
            int id = ((AccountModel)Session["User"]).Id;
            ((AccountModel)Session["User"]).PhoneNumber = null;
            int recordsUpdated = DeletePhoneNumber(id);
            return View();
        }

        public ActionResult EnableTwoFactorAuthentication()
        {
            string phoneNumber = ((AccountModel)Session["User"]).PhoneNumber;
            if (phoneNumber == null)
            {
                TempData["Error"] = "Please add a phone number to the account to enable this feature!";
                return RedirectToAction("Index");
            }
            else
            {
                int id = ((AccountModel)Session["User"]).Id;
                ((AccountModel)Session["User"]).TwoFactor = true;
                int recordsUpdated = UpdateTwoFactor(id, true);
                return RedirectToAction("Index");
            }
        }
        public ActionResult DisableTwoFactorAuthentication()
        {
            int id = ((AccountModel)Session["User"]).Id;
            ((AccountModel)Session["User"]).TwoFactor = false;
            int recordsUpdated = UpdateTwoFactor(id, false);
            return RedirectToAction("Index");
        }
        
        public ActionResult SuccessPhoneNumber()
        {
            Session.Remove("Phone");
            Session.Remove("ExpectedCode");
            Session.Remove("SentMessage");
            return View();
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            string oldPassword = ((AccountModel)Session["User"]).Password;
            int id = ((AccountModel)Session["User"]).Id;
            if (ModelState.IsValid)
            {
                if (!string.Equals(oldPassword, model.Password))
                {
                    ViewBag.ErrorMessage = "Old password is not correct";
                    return View(model);
                }
                else
                {
                    int recordUpdated = UpdatePassword(
                        id,
                        model.NewPassword
                    );

                }
            }
            return View(model);
        }

        public ActionResult SuccessChangePassword()
        {
            return View();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AccountModel account = SearchAccount(model.EmailAddress);
                    
                    if (account == null)
                    {
                        ViewBag.ErrorMessage = "This email does not have a Maple Building Management account associated with it!";
                        return View();
                    }

                    var senderEmail = new MailAddress("propertymbm@gmail.com");
                    var receiverEmail = new MailAddress(model.EmailAddress);
                    var password = "mapleB@1";
                    string randomString = RandomString(8, true);
                    var sub = "Password reset information for Maple Building Management App";
                    var body = "Good day, " + account.FirstName + "!\n\nYour new password for the Maple Building Management App is " + randomString + "\n\nSincerely,\n\nMaple Building Management Admin";
                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(senderEmail.Address, password)
                    };
                    using (var mess = new MailMessage(senderEmail, receiverEmail)
                    {
                        Subject = sub,
                        Body = body
                    })
                    {
                        smtp.Send(mess);
                    }

                    int recordUpdated = UpdatePassword(
                        account.Id,
                        randomString
                    );

                    return RedirectToAction("SuccessForgotPassword");
                }
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "There seems to be an error with the email messaging system";
            }
            return View();
        }

        public ActionResult SuccessForgotPassword()
        {
            return View();
        }

        public string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }
    }
}