using EmailApp.Context;
using EmailApp.Entities;
using EmailApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.Controllers
{
    [Authorize]
    public class MessageController(AppDbContext _context,
                                   UserManager<AppUser> _userManager) : Controller
    {
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Messages
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == user.Id && !x.IsDeleted && !x.IsArchived)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Gelen Kutusu";
            return View(messages);
        }
        public async Task<IActionResult> Sendbox()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Messages
                .Include(x => x.Receiver)
                .Where(x => x.SenderId == user.Id && !x.IsDeleted && !x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Gönderilenler";
            return View(messages);
        }

        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var message = _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .FirstOrDefault(x => x.MessageId == id);

            if (message == null)
                return NotFound();

            if (message.ReceiverId != user.Id && message.SenderId != user.Id)
                return Forbid();

            if (message.ReceiverId == user.Id && !message.IsRead)
            {
                message.IsRead = true;
                _context.SaveChanges();
            }

            return View(message);
        }

        public IActionResult SendMessage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model, string submitType)
        {
            var sender = await _userManager.FindByNameAsync(User.Identity.Name);

            var message = new Message
            {
                SenderId = sender.Id,
                Subject = model.Subject,
                Body = model.Body,
                SendDate = DateTime.Now,
                IsDeleted = false,
                IsImportant = false,
                IsArchived = false,
                IsRead = false
            };

            if (submitType == "Send")
            {
                var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
                if (receiver == null)
                {
                    ModelState.AddModelError("", "Alıcı bulunamadı.");
                    return View(model);
                }

                message.ReceiverId = receiver.Id;
                message.IsDraft = false;
            }
            else if (submitType == "Draft")
            {
                message.IsDraft = true;
                message.ReceiverId = null; // draft için boş olabilir
            }

            _context.Messages.Add(message);
            _context.SaveChanges();
            ViewBag.PageTitle = "Yeni Mesaj Gönder";
            return RedirectToAction("Inbox");
        }
        public async Task<IActionResult> Important()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var messages = _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => (x.ReceiverId == user.Id || x.SenderId == user.Id)
                            && x.IsImportant && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Önemli";
            return View(messages);
        }
        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var drafts = _context.Messages
                .Include(x => x.Receiver)
                .Include(x => x.Sender)
                .Where(x => x.SenderId == user.Id && x.IsDraft && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Taslaklar";
            return View(drafts);
        }
        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var trash = _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => (x.SenderId == user.Id || x.ReceiverId == user.Id) && x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Çöp Kutusu";
            return View(trash);
        }
        public async Task<IActionResult> Unread()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var unread = _context.Messages
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == user.Id && !x.IsRead && !x.IsDeleted && !x.IsDraft && !x.IsArchived)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Okunmamışlar";
            return View(unread);
        }
        public async Task<IActionResult> Archive()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var archive = _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.ReceiverId == user.Id && x.IsArchived && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToList();
            ViewBag.PageTitle = "Arşiv";
            return View(archive);
        }

        [HttpPost]
        public IActionResult ToggleArchive(int id,string returnUrl)
        {
            var message = _context.Messages.FirstOrDefault(x => x.MessageId == id);
            if (message != null)
            {
                message.IsArchived = !message.IsArchived;
                _context.SaveChanges();
            }
            return Redirect(returnUrl ?? Url.Action("Inbox"));
        }

        [HttpPost]
        public IActionResult ToggleImportant(int id, string returnUrl)
        {
            var message = _context.Messages.FirstOrDefault(x => x.MessageId == id);
            if (message != null)
            {
                message.IsImportant = !message.IsImportant;
                _context.SaveChanges();
            }
            return Redirect(returnUrl ?? Url.Action("Inbox"));
        }
        [HttpPost]
        public IActionResult DeleteSelected(List<int> selectedIds, string returnUrl)
        {
            if (selectedIds != null && selectedIds.Any())
            {
                var messages = _context.Messages
                    .Where(m => selectedIds.Contains(m.MessageId))
                    .ToList();

                foreach (var msg in messages)
                {
                    msg.IsDeleted = true;
                }

                _context.SaveChanges();
            }

            return Redirect(returnUrl ?? Url.Action("Inbox"));
        }
        [HttpPost]
        public IActionResult DeleteForever(List<int> selectedIds)
        {
            if (selectedIds != null && selectedIds.Any())
            {
                foreach (var id in selectedIds)
                {
                    var message = _context.Messages.FirstOrDefault(m => m.MessageId == id);
                    if (message != null)
                    {
                        _context.Messages.Remove(message); 
                    }
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Trash");
        }


    }
}
