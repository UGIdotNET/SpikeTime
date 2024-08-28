using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace UGIdotNET.SpikeTime.AwsCognito.Pages;

public class ConfirmModel : PageModel
{
    private readonly CognitoUserManager<CognitoUser> _userManager;

    public ConfirmModel(UserManager<CognitoUser> userManager)
    {
        _userManager = userManager as CognitoUserManager<CognitoUser> ?? throw new ArgumentNullException(nameof(userManager));
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
    }

    public void OnGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (ModelState.IsValid)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userEmail}'.");
            }

            var result = await _userManager.ConfirmSignUpAsync(user, Input.Code, true);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Error confirming account for user with ID '{userEmail}':");
            }
            else
            {
                return returnUrl != null ? LocalRedirect(returnUrl) : Page() as IActionResult;
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
