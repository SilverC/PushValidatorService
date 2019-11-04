using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushValidator.Data;
using PushValidator.Models;
using PushValidator.Models.ApplicationViewModels;

namespace PushValidator
{
    [Authorize]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const string RegisterUriFormat = "https://pushvalidator.com/register?secret={0}";

        public ApplicationsController(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Applications
        public async Task<IActionResult> Index()
        {
            return View(
                await _context.Applications.Where(m =>
                    m.UserId == _userManager.GetUserId(User)
                ).ToListAsync()
            );
        }

        // GET: Applications/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationModel = await _context.Applications
                .FirstOrDefaultAsync(m =>
                        m.Id == id &&
                        m.UserId == _userManager.GetUserId(User)
                );
            if (applicationModel == null)
            {
                return NotFound();
            }

            return View(applicationModel);
        }

        // GET: Applications/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Applications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddApplicationViewModel applicationModel)
        {
            if (ModelState.IsValid)
            {
                // Create 256 bit random key
                byte[] bytes = new byte[32];
                _rng.GetBytes(bytes);
                var key = Convert.ToBase64String(bytes);

                var model = new ApplicationModel
                {
                    Id = Guid.NewGuid(),
                    UserId = _userManager.GetUserId(User),
                    Key = key,
                    Name = applicationModel.Name
                };

                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(applicationModel);
        }

        // GET: Applications/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationModel = await _context.Applications.FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.UserId == _userManager.GetUserId(User)
            );
            if (applicationModel == null)
            {
                return NotFound();
            }
            return View(applicationModel);
        }

        // POST: Applications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ApplicationModel applicationModel)
        {
            if (id != applicationModel.Id)
            {
                return NotFound();
            }

            var result = await _context.Applications
                .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.UserId == _userManager.GetUserId(User)
                );
            if (ModelState.IsValid && result != null)
            {
                try
                {
                    _context.Update(applicationModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationModelExists(applicationModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(applicationModel);
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationModel = await _context.Applications
                .FirstOrDefaultAsync(m =>
                        m.Id == id &&
                        m.UserId == _userManager.GetUserId(User)
                );
            if (applicationModel == null)
            {
                return NotFound();
            }

            return View(applicationModel);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var applicationModel = await _context.Applications
                .FirstOrDefaultAsync(m =>
                        m.Id == id &&
                        m.UserId == _userManager.GetUserId(User)
                );
            _context.Applications.Remove(applicationModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationModelExists(Guid id)
        {
            return _context.Applications.Any(e =>
                    e.Id == id &&
                    e.UserId == _userManager.GetUserId(User)
            );
        }
    }
}
