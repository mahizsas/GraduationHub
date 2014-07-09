﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using AutoMapper.QueryableExtensions;
using DataTables.Mvc;
using GraduationHub.Web.Data;
using GraduationHub.Web.Domain;
using GraduationHub.Web.Filters;
using GraduationHub.Web.Infrastructure;
using GraduationHub.Web.Infrastructure.Alerts;
using GraduationHub.Web.Models.Invitations;

namespace GraduationHub.Web.Controllers
{
    public class InvitationsController : AppBaseController
    {
        private readonly ApplicationDbContext _context;

        public InvitationsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: Invitations
        public ActionResult Index()
        {
            //return View(await _context.Invitations.Project().To<InvitationIndexViewModel>().ToListAsync());
            return View();
        }

        public async Task<ActionResult> IndexTable([ModelBinder(typeof(DataTablesBinder))] IDataTablesRequest requestModel)
        {
            // Data
            var data = await _context.Invitations
                .Project().To<InvitationIndexViewModel>().ToListAsync();

            int totalRecords = data.Count();

            var paged = data.Skip(requestModel.Start).Take(requestModel.Length);

            var response = new DataTablesResponse(requestModel.Draw, paged, totalRecords, totalRecords);

            return JsonSuccess(response, JsonRequestBehavior.AllowGet);
        }

        // GET: Invitations/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Invitation invitation = await _context.Invitations.FindAsync(id);
            if (invitation == null)
            {
                return HttpNotFound();
            }
            return View(invitation);
        }

        // GET: Invitations/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Invitations/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken, Log("Create Invitation")]
        public async Task<ActionResult> Create(InvitationCreateFormModel formModel)
        {
            if (!ModelState.IsValid) return View(formModel);

            // If the email is already being used to authenticate into the system, it cannot be used
            bool userExists = await _context.Users
                .AnyAsync(u => u.Email.Equals(formModel.Email, StringComparison.OrdinalIgnoreCase));


            if (userExists)
            {
                return RedirectToAction<InvitationsController>(c => c.Index())
                    .WithError("It appears there is an existing User with this email address.");
            }
            // if there is an invitation for the same year, same email, this is invalid.
            bool invitationExists = await _context.Invitations.AnyAsync(i =>
                i.Email.Equals(formModel.Email, StringComparison.OrdinalIgnoreCase)
                && i.GraduatingClassId == formModel.GraduatingClassId)
                ;

            if (invitationExists)
            {
                return RedirectToAction<InvitationsController>(c => c.Index())
                    .WithError("It appears that this email has an invitation already.");
            }


            var invitation = new Invitation
            {
                StudentName = formModel.StudentName,
                GraduatingClassId = formModel.GraduatingClassId,
                Email = formModel.Email
            };

            _context.Invitations.Add(invitation);

            await _context.SaveChangesAsync();

            return RedirectToAction<InvitationsController>(c => c.Index()).WithSuccess("Invitation Created");
        }

        // GET: Invitations/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InvitationEditFormModel invitation =
                await _context.Invitations.Project().To<InvitationEditFormModel>()
                    .SingleOrDefaultAsync(i => i.Id == id.Value);

            if (invitation == null)
            {
                return RedirectToAction<InvitationsController>(c => c.Index())
                    .WithError("Could not load the Invitation.");
            }
            return View(invitation);
        }

        // POST: Invitations/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken, Log("Edited Invitation")]
        public async Task<ActionResult> Edit(InvitationEditFormModel editModel)
        {
            if (ModelState.IsValid)
            {
                // If the email is already being used to authenticate into the system, it cannot be used
                bool userExists = await _context.Users
                    .AnyAsync(u => u.Email.Equals(editModel.Email, StringComparison.OrdinalIgnoreCase));


                if (userExists)
                {
                    return RedirectToAction<InvitationsController>(c => c.Index())
                        .WithError("It appears there is an existing User with this email address.");
                }

                // if there is an invitation for the same year, same email, this is invalid.
                bool invitationExists = await _context.Invitations.AnyAsync(i =>
                    i.Email.Equals(editModel.Email, StringComparison.OrdinalIgnoreCase)
                    && i.GraduatingClassId == editModel.GraduatingClassId
                    && i.Id != editModel.Id);

                if (invitationExists)
                {
                    return RedirectToAction<InvitationsController>(c => c.Index())
                        .WithError("It appears that this email has an invitation already.");
                }
                Invitation invitation = await _context.Invitations.SingleOrDefaultAsync(i => i.Id == editModel.Id);

                if (invitation == null)
                {
                    return RedirectToAction<InvitationsController>(c => c.Index())
                        .WithError("Could not load the Invitation.");
                }

                invitation.GraduatingClassId = editModel.GraduatingClassId;
                invitation.StudentName = editModel.StudentName;
                invitation.Email = editModel.Email;
                
                _context.Entry(invitation).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();
                return RedirectToAction<InvitationsController>(c => c.Index()).WithSuccess("Invitation updated.");

            }

            return View(editModel);
        }

        // GET: Invitations/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Invitation invitation = await _context.Invitations.FindAsync(id);
            if (invitation == null)
            {
                return HttpNotFound();
            }
            return View(invitation);
        }

        // POST: Invitations/Delete/5
        [HttpPost, ActionName("Delete"), Log("Deleted Invitation")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Invitation invitation = await _context.Invitations.FindAsync(id);
            _context.Invitations.Remove(invitation);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}