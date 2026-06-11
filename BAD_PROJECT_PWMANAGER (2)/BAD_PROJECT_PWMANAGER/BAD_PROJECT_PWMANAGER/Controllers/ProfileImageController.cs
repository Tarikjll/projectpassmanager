using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BAD_PROJECT_PWMANAGER.Controllers;

[Authorize]
public class ProfileImageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileImageController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Current()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null ||
            user.ProfileImageData == null ||
            user.ProfileImageData.Length == 0 ||
            string.IsNullOrWhiteSpace(user.ProfileImageContentType))
        {
            return RedirectToAction(nameof(Default));
        }

        return File(user.ProfileImageData, user.ProfileImageContentType);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Default()
    {
        var defaultPath = Path.Combine(
            _webHostEnvironment.WebRootPath,
            "images",
            "default-avatar.jpg");

        if (!System.IO.File.Exists(defaultPath))
        {
            return NotFound();
        }

        return PhysicalFile(defaultPath, "image/jpg");
    }
}