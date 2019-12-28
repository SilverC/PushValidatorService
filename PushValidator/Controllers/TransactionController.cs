using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
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
        public async Task<IActionResult> LoginAttempt([FromBody] AddTransactionModel model)
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

            // Retrieve user by username in order to get user's device
            var user = await _userManager.FindByNameAsync(model.UserName);
            var device = _dbContext.Devices.FirstOrDefault(x => x.UserId == user.Id);

            // TODO: Send push notification to device.

            return Ok(transactionId);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFakeTransaction(Guid transactionId)
        {
            var transaction = new TransactionModel
            {
                Id = transactionId,
                UserId = _userManager.Users.First().Id,
                ApplicationId = Guid.Parse("18639afc-0e3d-4e44-a5ea-4201edb79912"),
                ClientIP = "192.168.1.1",
                GeoLocation = "IDK",
                Signature = "Nope",
                UserName = "BOB"
            };
            await _dbContext.AddAsync(transaction);
            var result = await _dbContext.SaveChangesAsync();

            if (result != 1)
            {
                return StatusCode(500);
            }

            // TODO: Return transaction id to web app to allow querying of result
            return Ok(transactionId);
        }

        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitAuthenticationResult([FromBody] AddAuthenticationResultModel model)
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

            var authenticationResult = _dbContext.AuthenticationResults
                                                 .FirstOrDefault(a => a.TransactionId == model.TransactionId);
            if(authenticationResult != null)
            {
                return BadRequest("Authentication result already created for this transaction id");
            }


            // TODO: Figure out how to use device public key to verify signed data
            var verification = model.VerifySignature(device.PublicKey);
            if(!verification)
            {
                return BadRequest();
            }

            authenticationResult = new AuthenticationResultModel
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

        //TODO: REMOVE, TEST METHODs
        private static void PrintByteArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write($"{array[i]:X2}");
                if ((i % 4) == 3) Console.Write(" ");
            }
            Console.WriteLine();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult TestVerify([FromBody] TestVerifyModel model)
        {
            var publicKeyBytes = Convert.FromBase64String(model.Key);
            var signatureBytes = Convert.FromBase64String(model.Signature);

            PrintByteArray(publicKeyBytes);
            // No need to parse public key using bouncy castle
            //var pubKeyX = publicKeyBytes.Skip(27).Take(32).ToArray();
            //PrintByteArray(pubKeyX);
            //var pubKeyY = publicKeyBytes.Skip(59).ToArray();
            //PrintByteArray(pubKeyY);

            //var ecdsaParams = new ECParameters
            //{
            //    Curve = ECCurve.NamedCurves.nistP256,
            //    Q = new ECPoint
            //    {
            //        X = pubKeyX,
            //        Y = pubKeyY
            //    }
            //};
            //ecdsaParams.Validate();


            Console.WriteLine("Signature:");
            PrintByteArray(signatureBytes);
            Console.WriteLine("-------------------------");
            var dataBytes = Encoding.UTF8.GetBytes(model.Data);
            Console.WriteLine("Data: ");
            PrintByteArray(dataBytes);
            Console.WriteLine("-------------------------");
            // No need to parse signature using bouncy castle.
            //Console.WriteLine("r: ");
            //var r = signatureBytes.Skip(4).Take(32).ToArray();
            //PrintByteArray(r);
            //Console.WriteLine("-------------------------");
            //Console.WriteLine("s: ");
            //var s = signatureBytes.Skip(39).ToArray();
            //PrintByteArray(s);
            //Console.WriteLine("-------------------------");
            //var parsedSignatureBytes = r.Concat(s).ToArray();
            //Console.WriteLine("Parsed Signature: ");
            //PrintByteArray(parsedSignatureBytes);
            //Console.WriteLine("-------------------------");

            var signerAlgorithm = "SHA256withECDSA";
            var signer = SignerUtilities.GetSigner(signerAlgorithm);
            var pubkey = PublicKeyFactory.CreateKey(publicKeyBytes);
            signer.Init(false, pubkey);
            signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
            var result = signer.VerifySignature(signatureBytes);

            // Using bouncy castle because it alleviates need to parse r & s from signature
            //var algorithm = ECDsa.Create(ecdsaParams);

            //algorithm.ImportParameters(ecdsaParams);
            //var result = algorithm.VerifyData(dataBytes,
            //                                parsedSignatureBytes,
            //                                HashAlgorithmName.SHA256);
            return Ok(result);

        }

        // TODO: Add HMAC protection for query
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckAuthentication(Guid transactionId)
        {
            var result = _dbContext.AuthenticationResults
                .FirstOrDefault(x => x.TransactionId == transactionId);
            if (result == null)
            {
                return NotFound();
            }

            return Json(result);
        }
    }
}
