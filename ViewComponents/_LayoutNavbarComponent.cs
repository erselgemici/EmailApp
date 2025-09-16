using EmailApp.Context;
using EmailApp.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.ViewComponents
{
    public class _LayoutNavbarComponent(AppDbContext _context, UserManager<AppUser> _userManager) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
      
            var unreadMessages = _context.Messages
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == user.Id && !x.IsRead && !x.IsDeleted && !x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .Take(3)
                .ToList();

            ViewBag.UnreadCount = _context.Messages
                .Count(x => x.ReceiverId == user.Id && !x.IsRead && !x.IsDraft && !x.IsDeleted && !x.IsArchived);

            ViewBag.UserFullName = $"{user.FirstName} {user.LastName}";

            return View(unreadMessages);
        }
    }
}
