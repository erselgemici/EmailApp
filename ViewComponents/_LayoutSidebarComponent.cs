using EmailApp.Context;
using EmailApp.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmailApp.ViewComponents
{
    public class _LayoutSidebarComponent(AppDbContext _context, UserManager<AppUser> _userManager) : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).Result;
            var userId = user.Id;

            ViewBag.InboxCount = _context.Messages.Count(x => x.ReceiverId == userId && !x.IsRead && !x.IsDraft && !x.IsDeleted && !x.IsArchived);
            ViewBag.SentCount = _context.Messages.Count(x => x.SenderId == userId && !x.IsDraft && !x.IsDeleted);
            ViewBag.DraftCount = _context.Messages.Count(x => x.SenderId == userId && x.IsDraft && !x.IsDeleted);
            ViewBag.ImportantCount = _context.Messages.Count(x => (x.ReceiverId == userId || x.SenderId == userId) && x.IsImportant && !x.IsDeleted);
            ViewBag.TrashCount = _context.Messages.Count(x => (x.ReceiverId == userId || x.SenderId == userId) && x.IsDeleted);
            ViewBag.ArchiveCount = _context.Messages.Count(x => x.ReceiverId == userId && x.IsArchived && !x.IsDeleted);
            ViewBag.UnreadCount = _context.Messages.Count(x => x.ReceiverId == userId && !x.IsRead && !x.IsDeleted && !x.IsDraft && !x.IsArchived);

            return View();
        }
    }
}
