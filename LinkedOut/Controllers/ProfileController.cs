using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LinkedOut.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace LinkedOut.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Custom JsonResult helper method to ensure consistent AJAX responses
        private JsonResult JsonResponse(bool success, string message = null, object data = null)
        {
            return new JsonResult(new
            {
                success = success,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Use projection to avoid CoverPhoto column
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        ProfilePicture = u.ProfilePicture,
                        Headline = u.Headline,
                        About = u.About,
                        Location = u.Location,
                        Website = u.Website
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                return View(user);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Profile Index error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Settings()
        {
            try
            {
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Use projection to avoid CoverPhoto column
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new 
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName, 
                        u.Email,
                        u.ProfilePicture,
                        u.Headline,
                        u.About,
                        u.Location,
                        u.Website,
                        CoverPhoto = string.Empty // Provide an empty default
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                // Model verilerini ViewBag'e ekle
                ViewBag.User = user;

                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Settings error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string email, string phone, string headline, string about)
        {
            System.Diagnostics.Debug.WriteLine("UpdateProfile method called");
            System.Diagnostics.Debug.WriteLine($"Received data: firstName={firstName}, lastName={lastName}, email={email}, headline={headline}");
            
            try
            {
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Validate input data
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "First name, last name, and email are required.";
                    return RedirectToAction("Settings");
                }
                
                // Log incoming data
                System.Diagnostics.Debug.WriteLine($"UpdateProfile: ID={userId}, FirstName={firstName}, LastName={lastName}, Email={email}, Headline={headline}");
                
                try
                {
                    // Direct SQL approach to update user data
                    var updateQuery = @"
                        UPDATE Users 
                        SET FirstName = @firstName, 
                            LastName = @lastName, 
                            Email = @email, 
                            Headline = @headline, 
                            About = @about
                        WHERE Id = @userId";

                    var parameters = new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@firstName", firstName),
                        new Microsoft.Data.SqlClient.SqlParameter("@lastName", lastName),
                        new Microsoft.Data.SqlClient.SqlParameter("@email", email),
                        new Microsoft.Data.SqlClient.SqlParameter("@headline", headline ?? string.Empty),
                        new Microsoft.Data.SqlClient.SqlParameter("@about", about ?? string.Empty),
                        new Microsoft.Data.SqlClient.SqlParameter("@userId", userId)
                    };

                    var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updateQuery, parameters);
                    
                    System.Diagnostics.Debug.WriteLine($"SQL Update completed: {rowsAffected} rows affected.");
                    
                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Profile updated successfully!";
                        return RedirectToAction("Settings");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No rows were updated. User possibly not found.");
                        TempData["ErrorMessage"] = "No changes were made. Please try again.";
                        return RedirectToAction("Settings");
                    }
                }
                catch (Exception sqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL Error: {sqlEx.Message}");
                    
                    // Fallback to entity update if SQL approach fails
                    System.Diagnostics.Debug.WriteLine("Trying entity approach...");
                    
                    // Load only necessary user data
                    var query = "SELECT Id, FirstName, LastName, Email, Headline, About FROM Users WHERE Id = @p0";
                    var user = await _context.Users.FromSqlRaw(query, userId).FirstOrDefaultAsync();
                    
                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine("User not found in fallback approach.");
                        TempData["ErrorMessage"] = "User not found. Please try again later.";
                        return RedirectToAction("Settings");
                    }
                    
                    // Manual property updates
                    user.FirstName = firstName;
                    user.LastName = lastName;
                    user.Email = email;
                    user.Headline = headline ?? string.Empty;
                    user.About = about;
                    
                    try
                    {
                        _context.Entry(user).Property(u => u.FirstName).IsModified = true;
                        _context.Entry(user).Property(u => u.LastName).IsModified = true;
                        _context.Entry(user).Property(u => u.Email).IsModified = true;
                        _context.Entry(user).Property(u => u.Headline).IsModified = true;
                        _context.Entry(user).Property(u => u.About).IsModified = true;
                        
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine("Entity update successful.");
                        TempData["SuccessMessage"] = "Profile updated successfully!";
                        return RedirectToAction("Settings");
                    }
                    catch (Exception entityEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Entity approach also failed: {entityEx.Message}");
                        throw; // Rethrow to outer catch
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"UpdateProfile error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again later.";
                return RedirectToAction("Settings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(IFormFile profileImage)
        {
            try
            {
                if (profileImage == null || profileImage.Length == 0)
                {
                    TempData["ErrorMessage"] = "No file selected";
                    return RedirectToAction("Settings");
                }

                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Log for debugging
                System.Diagnostics.Debug.WriteLine($"Uploading profile image for user ID={userId}, File size={profileImage.Length}");
                
                // Create directory if it doesn't exist
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    System.Diagnostics.Debug.WriteLine($"Created directory: {uploadsFolder}");
                }

                // Generate unique filename
                string uniqueFileName = $"{userId}_{Guid.NewGuid().ToString()}{Path.GetExtension(profileImage.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                string fileUrl = $"/uploads/profiles/{uniqueFileName}";
                
                System.Diagnostics.Debug.WriteLine($"Saving file to: {filePath}");

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }
                
                System.Diagnostics.Debug.WriteLine($"File saved successfully");

                // Update the database using SQL to avoid CoverPhoto column
                var updateQuery = "UPDATE Users SET ProfilePicture = @profilePicture WHERE Id = @userId";
                var parameters = new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@profilePicture", fileUrl),
                    new Microsoft.Data.SqlClient.SqlParameter("@userId", userId)
                };
                
                await _context.Database.ExecuteSqlRawAsync(updateQuery, parameters);
                System.Diagnostics.Debug.WriteLine($"Profile picture URL updated in database: {fileUrl}");

                TempData["SuccessMessage"] = "Profile image updated successfully";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"UpdateProfileImage error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "Failed to update profile image: " + ex.Message;
                return RedirectToAction("Settings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCoverImage(IFormFile coverImage)
        {
            try
            {
                if (coverImage == null || coverImage.Length == 0)
                {
                    TempData["ErrorMessage"] = "No file selected";
                    return RedirectToAction("Settings");
                }

                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                System.Diagnostics.Debug.WriteLine($"Uploading cover image for user ID={userId}, File size={coverImage.Length}");

                // Create directory if it doesn't exist
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/covers");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    System.Diagnostics.Debug.WriteLine($"Created directory: {uploadsFolder}");
                }

                // Generate unique filename
                string uniqueFileName = $"{userId}_{Guid.NewGuid().ToString()}{Path.GetExtension(coverImage.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                string fileUrl = $"/uploads/covers/{uniqueFileName}";
                
                System.Diagnostics.Debug.WriteLine($"Saving file to: {filePath}");

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }
                
                System.Diagnostics.Debug.WriteLine($"File saved successfully");

                try {
                    // Check if CoverPhoto column exists
                    var columnCheck = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CoverPhoto'";
                    var columnExists = await _context.Database.ExecuteSqlRawAsync(columnCheck) > 0;
                    
                    if (columnExists) {
                        // Update using SQL
                        var updateQuery = "UPDATE Users SET CoverPhoto = @coverPhoto WHERE Id = @userId";
                        var parameters = new[] {
                            new Microsoft.Data.SqlClient.SqlParameter("@coverPhoto", fileUrl),
                            new Microsoft.Data.SqlClient.SqlParameter("@userId", userId)
                        };
                        
                        await _context.Database.ExecuteSqlRawAsync(updateQuery, parameters);
                        System.Diagnostics.Debug.WriteLine($"Cover photo URL updated in database: {fileUrl}");
                    } else {
                        System.Diagnostics.Debug.WriteLine("CoverPhoto column doesn't exist, skipping database update");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue - we still want to return the URL
                    System.Diagnostics.Debug.WriteLine($"Error updating cover photo in database: {ex.Message}");
                }

                TempData["SuccessMessage"] = "Cover image updated successfully";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"UpdateCoverImage error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "Failed to update cover image: " + ex.Message;
                return RedirectToAction("Settings");
            }
        }
        
        // Add method to view another user's profile
        public async Task<IActionResult> UserProfile(int id)
        {
            try
            {
                // If no ID provided, show current user's profile
                if (id == 0)
                {
                    return RedirectToAction("Index");
                }
                
                // Get current user ID to check if viewing own profile
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (id == currentUserId)
                {
                    // Redirect to own profile
                    return RedirectToAction("Index");
                }
                
                // Get requested user profile
                var user = await _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        ProfilePicture = u.ProfilePicture,
                        Headline = u.Headline,
                        About = u.About,
                        Location = u.Location,
                        Website = u.Website
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                // Pass whether this is the current user to the view
                ViewBag.IsCurrentUser = false;
                
                return View("Index", user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserProfile error: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
