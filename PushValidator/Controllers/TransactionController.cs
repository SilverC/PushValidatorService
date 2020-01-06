using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using PushSharp.Apple;
using PushValidator.Data;
using PushValidator.Models;
using PushValidator.Models.APIModels;
using PushValidator.Library;
using Microsoft.Extensions.Options;

namespace PushValidator.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<APNSConfiguration> _apnsConfiguration;

        public TransactionController(ApplicationDbContext dbContext,
                                     UserManager<ApplicationUser> userManager,
                                     IOptions<APNSConfiguration> apnsConfiguration)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _apnsConfiguration = apnsConfiguration;
        }

        // GET: /<controller>/
        [Authorize]
        public IActionResult Index()
        {
            ViewData["APNSPATH"] = _apnsConfiguration.Value.CertificatePath;
            ViewData["APNSPASS"] = _apnsConfiguration.Value.CertificatePassword.Substring(2, 5);
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
            if (user == null)
            {
                return BadRequest("No matching user found.");
            }
            var device = _dbContext.Devices.FirstOrDefault(x => x.UserId == user.Id);
            if (device == null)
            {
                return BadRequest("No device found for user.");
            }

            //TODO: Implement geolocation of IP
            var notification = new LoginNotificationModel
            {
                TransactionId = transaction.Id,
                ApplicationName = application.Name,
                UserName = transaction.UserName,
                ClientIP = transaction.ClientIP,
                GeoLocation = "Not yet implemented",
                TimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
            };

            SendNotification(device.DeviceToken, notification);

            return Ok(transactionId);
        }

        private bool SendNotification(string deviceToken, LoginNotificationModel model)
        {
            // Configuration(NOTE: .pfx can also be used here)
            var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Sandbox,
                                               _apnsConfiguration.Value.CertificatePath,
                                               _apnsConfiguration.Value.CertificatePassword);

            // Create a new broker
            var apnsBroker = new ApnsServiceBroker(config);

            // Wire up events
            apnsBroker.OnNotificationFailed += (notification, aggregateEx) => {

                aggregateEx.Handle(ex => {

                    // See what kind of exception it was to further diagnose
                    if (ex is ApnsNotificationException notificationException)
                    {

                        // Deal with the failed notification
                        var apnsNotification = notificationException.Notification;
                        var statusCode = notificationException.ErrorStatusCode;

                        Console.WriteLine($"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}");

                    }
                    else
                    {
                        // Inner exception might hold more useful information like an ApnsConnectionException			
                        Console.WriteLine($"Apple Notification Failed for some unknown reason : {ex.InnerException}");
                    }

                    // Mark it as handled
                    return true;
                });
            };

            apnsBroker.OnNotificationSucceeded += (notification) => {
                Console.WriteLine("Apple Notification Sent!");
            };

            // Start the broker
            apnsBroker.Start();
            var json = model.ToAPNSNotification();
            var test = JObject.Parse("{\"aps\":{ \"alert\":\"Authentication Request\" },\"ApplicationName\":\"Sample .Net App\",\"UserName\": \"admin@test.com\",\"ClientIp\": \"192.168.1.10\",\"GeoLocation\": \"Harrisonburg, VA, USA\",\"TransactionId\": \"ea690fa5-8454-4909-902f-3f1b69db9119\",\"Timestamp\": 1577515106.505353}");

            apnsBroker.QueueNotification(new ApnsNotification
            {
                DeviceToken = deviceToken,
                Payload = json
                //Payload = JObject.Parse("{\"aps\":{ \"alert\":\"Authentication Request\" },\"ApplicationName\":\"Sample .Net App\",\"UserName\": \"admin@test.com\",\"ClientIp\": \"192.168.1.10\",\"GeoLocation\": \"Harrisonburg, VA, USA\",\"TransactionId\": \"ea690fa5-8454-4909-902f-3f1b69db9119\",\"Timestamp\": 1577515106.505353}")
            });

            // Stop the broker, wait for it to finish   
            // This isn't done after every message, but after you're
            // done with the broker
            apnsBroker.Stop();
            return false;
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
        public async Task<IActionResult> CheckAuthentication(Guid transactionId)
        {
            var result = _dbContext.AuthenticationResults
                .FirstOrDefault(x => x.TransactionId == transactionId);
            if (result == null)
            {
                return NotFound();
            }

            var transaction = await _dbContext.Transactions.FindAsync(result.TransactionId);
            if(transaction == null)
            {
                return NotFound();
            }

            var application = await _dbContext.Applications.FindAsync(transaction.ApplicationId);
            if (application == null)
            {
                return NotFound();
            }

            var model = new GetAuthenticationResultModel
            {
                ActualClientIP = result.ActualClientIP,
                CertificateFingerprint = result.CertificateFingerprint,
                ClientIPMatch = result.ClientIPMatch,
                Result = result.Result,
                ServerIP = result.ServerIP,
                ServerURI = result.ServerURI,
                TransactionId = result.TransactionId
            };

            model.Signature = Convert.ToBase64String(model.CalculateSignature(application.Key));

            return Json(model);
        }
    }
}
