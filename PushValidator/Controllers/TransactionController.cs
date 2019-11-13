using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PushValidator.Data;
using PushValidator.Models;
using PushValidator.Models.APIModels;

namespace PushValidator.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(ApplicationDbContext dbContext,
                                 UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAttempt(AddTransactionModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            var application = await _dbContext.Applications.FindAsync(model.ApplicationId);
            if(application == null)
            {
                return NotFound();
            }

            var signature = model.CalculateSignature(application.Key);
            var providedSignature = Convert.FromBase64String(model.Signature);
            if (!signature.SequenceEqual(providedSignature))
            {
                return BadRequest();
            }

            var transactionId = Guid.NewGuid();
            var transaction = new TransactionModel
            {
                Id = transactionId,
                UserId = application.UserId,
                ApplicationId = application.Id,
                ClientIP = model.ClientIP,
                GeoLocation = model.GeoLocation,
                Signature = model.Signature,
                UserName = model.UserName
            };
            await _dbContext.AddAsync(transaction);
            var result = await _dbContext.SaveChangesAsync();

            if(result != 1)
            {
                return StatusCode(500);
            }

            // TODO: Return transaction id to web app to allow querying of result
            return Ok(transactionId);
        }

        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitAuthenticationResult(AddAuthenticationResultModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            var transaction = await _dbContext.Transactions.FindAsync(model.TransactionId);
            if (transaction == null)
            {
                return NotFound();
            }

            var device = _dbContext.Devices.FirstOrDefault(d => d.UserId == transaction.UserId);
            if (device == null)
            {
                return NotFound();
            }

            // TODO: Figure out how to use device public key to verify signed data
            var signature = Convert.FromBase64String(device.PublicKey);
            var verification = model.VerifySignature(signature);
            if(!verification)
            {
                return BadRequest();
            }

            var authenticationResult = new AuthenticationResultModel
            {
                ActualClientIP = model.ActualClientIP,
                CertificateFingerprint = model.CertificateFingerprint,
                ClientIPMatch = model.ClientIPMatch,
                Result = model.Result,
                ServerIP = model.ServerIP,
                ServerURI = model.ServerURI,
                TransactionId = model.TransactionId
            };

            await _dbContext.AddAsync(authenticationResult);
            var result = await _dbContext.SaveChangesAsync();
            if(result != 1)
            {
                return StatusCode(500);
            }

            // Web apps will continue to query the database using the original transaction id

            return Ok();
        }

        // TODO: Add ability to query results
    }
}
