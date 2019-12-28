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
using PushValidator.Models.DeviceViewModels;

namespace PushValidator.Controllers
{
    [Authorize]
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const string RegisterUriFormat = "https://pushvalidator.com/register?secret={0}&id={1}";

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

        // GET: Devices/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _dbContext.Devices
                .FirstOrDefaultAsync(d =>
                        d.Id == id &&
                        d.UserId == _userManager.GetUserId(User)
                );
            if (device == null)
            {
                return NotFound();
            }

            var model = new ViewDeviceViewModel
            {
                RegisterURI = string.Format(RegisterUriFormat, device.SymmetricKey, device.Id),
                Id = device.Id,
                PublicKey = device.PublicKey,
                SymmetricKey = device.SymmetricKey,
                DeviceToken = device.DeviceToken,
                Name = device.Name,
                Registered = device.Registered,
                UserId = device.UserId
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Add()
        {
            // Create 256 bit random key
            byte[] bytes = new byte[32];
            _rng.GetBytes(bytes);
            var key = Convert.ToBase64String(bytes);
            var id = Guid.NewGuid().ToString();
            var registerURI = string.Format(RegisterUriFormat, key, id);

            var model = new AddDeviceViewModel
            {
                Id = id,
                SymmetricKey = key,
                RegisterURI = registerURI
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
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromBody] UpdateDeviceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var device = _dbContext.Devices.Find(new Guid(model.DeviceId));
                if (device == null)
                {
                    // If device can't be located in the database return 404
                    return NotFound();
                }

                if (device.Registered)
                {
                    // If device has already been registered then it can't be re-registered
                    // i.e. the public key and device token are immutable
                    return BadRequest();
                }

                // Plan to allow ModelState.IsValid perform HMAC validation to ensure correctness at this point
                // Initialize a HMAC object for calculating the hash from the Symmetric Key stored with the device
                // Calculate the HMAC of the submitted data against the provided HMAC to ensure the submitting entity has the symmetric key
                // If the HMAC is valid then the submission is authorized to update the device
                var key = Convert.FromBase64String(device.SymmetricKey);
                var hmac = new HMACSHA256(key);
                var computedHash = hmac.ComputeHash(model.GetCombinedByteString());
                var providedHash = Convert.FromBase64String(model.HMAC);
                if (!computedHash.SequenceEqual(providedHash))
                {
                    return BadRequest();
                }

                // Valid submission so apply the values to the database
                device.DeviceToken = model.DeviceToken;
                device.PublicKey = model.PublicKey;
                device.Registered = true;

                _dbContext.Update(device);
                var result = await _dbContext.SaveChangesAsync();

                if (result != 1)
                {
                    // Invalid update number
                    return StatusCode(500);
                    
                }

                // Update success
                return Ok();
            }

            // Model validation did not pass
            return BadRequest();
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid? id)
        {
            var device = await _dbContext.Devices
                .FirstOrDefaultAsync(m =>
                        m.Id == id &&
                        m.UserId == _userManager.GetUserId(User)
                );
            _dbContext.Devices.Remove(device);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //TODO: Remove this method. For testing only.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetHMAC(UpdateDeviceViewModel model)
        {
            var device = _dbContext.Devices.Find(new Guid(model.DeviceId));
            if (device == null)
            {
                // If device can't be located in the database return 404
                return NotFound();
            }

            var data = model.GetCombinedByteString();
            var key = Convert.FromBase64String(device.SymmetricKey);
            var hmac = new HMACSHA256(key);

            var result = new GetHMACModel
            {
                HMAC = Convert.ToBase64String(hmac.ComputeHash(data))
            };

            return View(result);
        }

    }
}
