using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace UGIdotNET.SpikeTime.AwsCognito.Pages;

public class RegisterModel : PageModel
{
    private readonly SignInManager<CognitoUser> _signInManager;

    private readonly CognitoUserManager<CognitoUser> _userManager;

    private readonly CognitoUserPool _pool;

    public RegisterModel(
        SignInManager<CognitoUser> signInManager,
        UserManager<CognitoUser> userManager,
        CognitoUserPool pool)
    {
        _signInManager = signInManager;
        _userManager = userManager as CognitoUserManager<CognitoUser> ?? throw new ArgumentNullException(nameof(userManager));
        _pool = pool;
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (ModelState.IsValid)
        {
            var user = _pool.GetUser(Input.UserName);
            user.Attributes.Add(CognitoAttribute.Email.AttributeName, Input.Email);

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToPage("./ConfirmAccount");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
