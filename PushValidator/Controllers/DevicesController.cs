using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PushValidator.Data;
using PushValidator.Models;
using PushValidator.Models.DeviceViewModels;

namespace PushValidator.Controllers
{
    [Authorize]
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public DevicesController(ApplicationDbContext dbContext,
                                 UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GET: /<controller>/
        [HttpGet]
        public IActionResult Index()
        {
            var devices = _dbContext.Devices.Where(
                            device => device.UserId == _userManager.GetUserId(User)
                          );
            var model = new ListDevicesViewModel
            {
                Devices = devices
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Add()
        {
            //TODO: Will need to generate the symmetric key client side
            // so the device can send the public key and device token afterwards with a HMAC
            var model = new AddDeviceViewModel
            {
                Id = Guid.NewGuid().ToString()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddDeviceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var device = new DeviceModel
                {
                    Name = model.Name,
                    SymmetricKey = model.SymmetricKey,
                    DeviceToken = null,
                    UserId = _userManager.GetUserId(User),
                    Registered = false,
                    Id = new Guid(model.Id)
                };

                await _dbContext.AddAsync(device);
                var result = await _dbContext.SaveChangesAsync();
                if (result == 1)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(model);
                }
            }
            else
            {
                // Model validation did not pass
                model.Id = Guid.NewGuid().ToString();
                return View(model);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateDeviceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var device = _dbContext.Devices.Find(new Guid(model.DeviceId));
                if (device == null)
                {
                    // If device can't be located in the database return 404
                    return NotFound();
                }

                // Plan to allow ModelState.IsValid perform HMAC validation to ensure correctness at this point
                device.DeviceToken = model.DeviceToken;
                device.PublicKey = model.PublicKey;
                device.Registered = true;

                _dbContext.Update(device);
                var result = await _dbContext.SaveChangesAsync();

                if(result == 1)
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(500);
                }
            }
            else
            {
                // Model validation did not pass
                return BadRequest();
            }
        }

    }
}
