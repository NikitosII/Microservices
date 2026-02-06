using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.API.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IIdentityProviderStore _identityProviderStore;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IAuthenticationSchemeProvider schemeProvider,
            IIdentityProviderStore identityProviderStore,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _events = events;
            _schemeProvider = schemeProvider;
            _identityProviderStore = identityProviderStore;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

                    if (context != null)
                    {
                        if (IsNativeClient(context))
                        {
                            return View("Redirect", new RedirectViewModel { RedirectUrl = model.ReturnUrl });
                        }

                        return Redirect(model.ReturnUrl);
                    }

                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        throw new Exception("invalid return URL");
                    }
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
                ModelState.AddModelError(string.Empty, "Invalid username or password");
            }

            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                return await Logout(vm);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost("api/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    CreatedTime = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");

                    _logger.LogInformation("User created a new account with password.");

                    return Ok(new { Message = "User created successfully" });
                }

                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registration");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("api/login")]
        public async Task<IActionResult> ApiLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, false, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if (user == null)
                    {
                        return BadRequest(new { Error = "User not found" });
                    }

                    // Return instructions for getting token
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    return Ok(new
                    {
                        Message = "Login successful. To get an access token, use the /connect/token endpoint",
                        TokenEndpoint = $"{baseUrl}/connect/token",
                        Instructions = "Use POST with form data: grant_type=password&username=" + request.Email + "&password=yourpassword&client_id=swagger-client&scope=openid profile product.api",
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    });
                }

                if (result.IsLockedOut)
                {
                    return BadRequest(new { Error = "Account locked out" });
                }

                return BadRequest(new { Error = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        private bool IsNativeClient(AuthorizationRequest context)
        {

            return context?.Client?.Properties?.ContainsKey("NativeClient") == true ||
                   context?.Client?.AllowedGrantTypes?.Contains(GrantType.DeviceFlow) == true ||
                   context?.Client?.AllowedGrantTypes?.Contains(GrantType.AuthorizationCode) == true;
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            return new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = true };

            if (User?.Identity.IsAuthenticated != true)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            return new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = true,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SecurityHeadersAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (!resultContext.HttpContext.Response.HasStarted)
            {
                resultContext.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                resultContext.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                resultContext.HttpContext.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            }
        }
    }
}
