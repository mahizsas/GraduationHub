﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AutoMapper.QueryableExtensions;
using GraduationHub.Web.Data;
using GraduationHub.Web.Domain;
using GraduationHub.Web.Filters;
using GraduationHub.Web.Infrastructure;
using GraduationHub.Web.Infrastructure.Alerts;
using GraduationHub.Web.Models.CheckList;

namespace GraduationHub.Web.Controllers
{
    [GraduationHubAuthorize(Roles = SecurityConstants.Roles.Student)]
    public class CheckListController : AppBaseController
    {
        private readonly ICurrentUser _currentUser;
        private readonly ApplicationDbContext _dbContext;

        public CheckListController(ApplicationDbContext dbContext, ICurrentUser currentUser)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
        }

        public ActionResult Biography()
        {
            return View();
        }

        public  ActionResult ExpressionOfThanks()
        {
            StudentExpressionModel studentExpression = _dbContext.StudentExpressions
                .Where(e => e.StudentId.Equals(_currentUser.User.Id))
                .Where(e => e.Type == StudentExpressionType.ThankYou).Project().To<StudentExpressionModel>()
                .SingleOrDefault() ?? new StudentExpressionModel();

            studentExpression.TextMaxLength = 100;

            return View(studentExpression);
        }

        // POST: FrequentlyAskedQuestions/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ExpressionOfThanks(StudentExpressionModel model)
        {
            if (!ModelState.IsValid) return View(model);

            StudentExpression studentExpression =  _dbContext.StudentExpressions
                .Where(e => e.StudentId.Equals(_currentUser.User.Id))
                .SingleOrDefault(e => e.Type == StudentExpressionType.ThankYou) ?? new StudentExpression {Type = StudentExpressionType.ThankYou};

            studentExpression.StudentId = _currentUser.User.Id;
            studentExpression.Text = model.Text;

            if (studentExpression.Id == default(int))
            {
                _dbContext.StudentExpressions.Add(studentExpression);
            }
            else
            {
                ObjectStateManager objectStateManager =
                    ((IObjectContextAdapter) _dbContext).ObjectContext.ObjectStateManager;
                _dbContext.StudentExpressions.Attach(studentExpression);
                objectStateManager.ChangeObjectState(studentExpression, EntityState.Modified);
            }

            _dbContext.SaveChanges();

            return RedirectToAction<CheckListController>(c => c.ExpressionOfThanks())
                .WithSuccess("Your Expression of Thanks was Saved.");
        }

        public ActionResult SlideShowCaption()
        {
            return View();
        }

        public ActionResult SeniorPortrait()
        {
            return View();
        }

        public ActionResult BabyPicture()
        {
            return View();
        }

        public ActionResult FirstYouthfulPicture()
        {
            return View();
        }

        public ActionResult SecondYouthfulPicture()
        {
            return View();
        }

        public ActionResult ThirdYouthfulPicture()
        {
            return View();
        }

        public ActionResult FourthYouthfulPicture()
        {
            return View();
        }

        public ActionResult ImportantDates()
        {
            // TODO: Add Student's Graduating Class in case we need to split large groups
            List<CheckListImportantDate> dueDates =
                _dbContext.ImportantDates.Project().To<CheckListImportantDate>().OrderBy(x => x.DueDate).ToList();

            return View(dueDates);
        }

        public ActionResult Faqs()
        {
            List<Faq> faqs = _dbContext.FrequentlyAskedQuestions.OrderBy(x => x.Order).Project().To<Faq>().ToList();

            return View(faqs);
        }
    }
}