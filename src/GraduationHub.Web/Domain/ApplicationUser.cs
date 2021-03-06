using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using GraduationHub.Web.Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace GraduationHub.Web.Domain
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(FieldLengths.ApplicationUser.FirstName)]
        public string FirstName { get; set; }

        [Required, StringLength(FieldLengths.ApplicationUser.LastName)]
        public string LastName { get; set; }

        public ICollection<StudentExpression> StudentExpressions { get; set; }

        public ICollection<StudentPicture> StudentPictures { get; set; }

        public bool IsStudent { get; set; }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(FirstName))
                {
                    return string.Format("{0} {1}", FirstName, LastName);
                }

                return Email;
            }
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }
}