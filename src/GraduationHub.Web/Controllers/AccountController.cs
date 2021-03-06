﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GraduationHub.Web.Domain;
using GraduationHub.Web.Infrastructure;
using GraduationHub.Web.Models.Account;
using GraduationHub.Web.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace GraduationHub.Web.Controllers
{
    [Authorize]
    public class AccountController : AppBaseController
    {
        private readonly IInvitationManager _invitationManager;
        private ApplicationUserManager _userManager;


        public AccountController(ApplicationUserManager userManager, IInvitationManager invitationManager,
            HttpContextBase httpContextBase)
        {
            _userManager = httpContextBase.GetOwinContext().GetUserManager<ApplicationUserManager>();
            _invitationManager = invitationManager;
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ApplicationUser user = await _userManager.FindAsync(model.Email, model.Password);
                    if (user != null)
                    {
                        await SignInAsync(user, model.RememberMe);
                        return RedirectToLocal(returnUrl);
                    }
                    ModelState.AddModelError("", "Invalid username or password.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register(string email = "", string inviteCode = "")
        {
            var model = new RegisterViewModel
            {
                Email = email,
                InviteCode = inviteCode
            };

            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            /*Validate*/
            Invitation invitation =
                await _invitationManager.GetInvitation(model.Email, model.InviteCode);

            // Invitation could not be found.
            if (invitation == null)
            {
                ModelState.AddModelError("",
                    "Could not locate Invitation. Please check your invitation email for the correct values.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true
            };


            IdentityResult result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Mark the Invitation as redeemed
                _invitationManager.RedeemCode(model.InviteCode);

                if (invitation.IsTeacher)
                {
                    await _userManager.AddToRoleAsync(user.Id, SecurityConstants.Roles.Teacher);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user.Id, SecurityConstants.Roles.Student);
                }


                await SignInAsync(user, false);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                return RedirectToAction<HomeController>(c => c.Index());
            }
            AddErrors(result);

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }

            IdentityResult result = await _userManager.ConfirmEmailAsync(userId, code);
            if (result.Succeeded)
            {
                return View("ConfirmEmail");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await _userManager.FindByNameAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user.Id)))
                {
                    ModelState.AddModelError("", "The user either does not exist or is not confirmed.");
                    return View();
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await _userManager.GeneratePasswordResetTokenAsync(user.Id);
                string callbackUrl = Url.Action("ResetPassword", "Account", new {userId = user.Id, code},
                    Request.Url.Scheme);


                var messageText = new StringBuilder();
                messageText.AppendLine("<p>Hello,</p>");
                messageText.AppendLine(
                    "<p>We received a request to reset the password associated with this email address. If you made this request, please follow the instructions below. If you did not request to have your password reset, you can safely ignore this email. We assure you that your account is safe.</p>");

                messageText.AppendLine("<p>Click this <a href=\"" + callbackUrl +
                                       "\">link</a> to reset your password.</p>");
                messageText.AppendLine(
                    "<p>If clicking the link doesn't work, you can copy and paste the link into your browser's address window. Once you have returned to KEYS GradHub, we will give you instructions for resetting your password.</p>");
                messageText.AppendLine("<p>Sincerely,<p>");
                messageText.AppendLine("<p>GradHub Admin</p>");

                await _userManager.SendEmailAsync(user.Id, "Reset Password", messageText.ToString());
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return View("Error");
            }
            return View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No user found.");
                    return View();
                }
                IdentityResult result = await _userManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                AddErrors(result);
                return View();
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess
                    ? "Your password has been changed."
                    : message == ManageMessageId.SetPasswordSuccess
                        ? "Your password has been set."
                        : message == ManageMessageId.RemoveLoginSuccess
                            ? "The external login was removed."
                            : message == ManageMessageId.Error
                                ? "An error has occurred."
                                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result =
                        await
                            _userManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                                model.NewPassword);
                    if (result.Succeeded)
                    {
                        ApplicationUser user = await _userManager.FindByIdAsync(User.Identity.GetUserId());
                        await SignInAsync(user, false);
                        return RedirectToAction("Manage", new {Message = ManageMessageId.ChangePasswordSuccess});
                    }
                    AddErrors(result);
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result =
                        await _userManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new {Message = ManageMessageId.SetPasswordSuccess});
                    }
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }
            base.Dispose(disposing);
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties {IsPersistent = isPersistent},
                await user.GenerateUserIdentityAsync(_userManager));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            ApplicationUser user = _userManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private void SendEmail(string email, string callbackUrl, string subject, string message)
        {
            // For information on sending mail, please visit http://go.microsoft.com/fwlink/?LinkID=320771
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}