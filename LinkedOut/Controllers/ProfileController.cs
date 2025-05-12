using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LinkedOut.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using BCrypt.Net;

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
                        u.Phone,
                        u.ProfilePicture,
                        u.Headline,
                        u.About,
                        u.Location,
                        u.Website,
                        u.CoverPhoto
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                // Load user's experiences
                var experiences = await _context.Experiences
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();
                
                // Load user's educations
                var educations = await _context.Educations
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();

                // Model verilerini ViewBag'e ekle
                ViewBag.User = user;
                ViewBag.Experiences = experiences;
                ViewBag.Educations = educations;

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
            System.Diagnostics.Debug.WriteLine($"Received data: firstName={firstName}, lastName={lastName}, email={email}, phone={phone}, headline={headline}");
            
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
                System.Diagnostics.Debug.WriteLine($"UpdateProfile: ID={userId}, FirstName={firstName}, LastName={lastName}, Email={email}, Phone={phone}, Headline={headline}");
                
                try
                {
                    // Direct SQL approach to update user data
                    var updateQuery = @"
                        UPDATE Users 
                        SET FirstName = @firstName, 
                            LastName = @lastName, 
                            Email = @email, 
                            Phone = @phone,
                            Headline = @headline, 
                            About = @about
                        WHERE Id = @userId";

                    var parameters = new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@firstName", firstName),
                        new Microsoft.Data.SqlClient.SqlParameter("@lastName", lastName),
                        new Microsoft.Data.SqlClient.SqlParameter("@email", email),
                        new Microsoft.Data.SqlClient.SqlParameter("@phone", phone ?? string.Empty),
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
                    var query = "SELECT Id, FirstName, LastName, Email, Phone, Headline, About FROM Users WHERE Id = @p0";
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
                    user.Phone = phone ?? string.Empty;
                    user.Headline = headline ?? string.Empty;
                    user.About = about;
                    
                    try
                    {
                        _context.Entry(user).Property(u => u.FirstName).IsModified = true;
                        _context.Entry(user).Property(u => u.LastName).IsModified = true;
                        _context.Entry(user).Property(u => u.Email).IsModified = true;
                        _context.Entry(user).Property(u => u.Phone).IsModified = true;
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
                
                // Generate file name and path
                var fileName = $"cover_{userId}_{DateTime.Now.Ticks}{Path.GetExtension(coverImage.FileName)}";
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Save file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(fileStream);
                }
                
                // Update database with new file path
                var webPath = $"/uploads/{fileName}";
                
                // Log what we're trying to do
                System.Diagnostics.Debug.WriteLine($"Updating cover image for user {userId} to {webPath}");
                
                try
                {
                    // Direct SQL approach to update only the CoverPhoto field
                    var updateQuery = @"
                        UPDATE Users 
                        SET CoverPhoto = @coverPhoto
                        WHERE Id = @userId";

                    var parameters = new[] {
                        new Microsoft.Data.SqlClient.SqlParameter("@coverPhoto", webPath),
                        new Microsoft.Data.SqlClient.SqlParameter("@userId", userId)
                    };

                    var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updateQuery, parameters);
                    
                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Cover photo updated successfully!";
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
                    System.Diagnostics.Debug.WriteLine($"SQL Error in UpdateCoverImage: {sqlEx.Message}");
                    
                    // Fallback to entity update
                    var user = await _context.Users.FindAsync(userId);
                    
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User not found. Please try again later.";
                        return RedirectToAction("Settings");
                    }
                    
                    user.CoverPhoto = webPath;
                    
                    try 
                    {
                        _context.Entry(user).Property(u => u.CoverPhoto).IsModified = true;
                        await _context.SaveChangesAsync();
                        
                        TempData["SuccessMessage"] = "Cover photo updated successfully!";
                        return RedirectToAction("Settings");
                    }
                    catch (Exception entityEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Entity approach also failed: {entityEx.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"UpdateCoverImage error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "An error occurred while updating your cover photo. Please try again later.";
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

        [HttpPost]
        public async Task<IActionResult> SaveExperience(Experience model, string StartDate, string EndDate)
        {
            try
            {
                // Debug incoming data
                System.Diagnostics.Debug.WriteLine($"SaveExperience: Received data - Company: {model.Company}, Position: {model.Position}, StartDate: {StartDate}, EndDate: {EndDate}, IsCurrentJob: {model.IsCurrentJob}");
                
                // Get the current user
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Validation
                if (string.IsNullOrEmpty(model.Company))
                {
                    TempData["ErrorMessage"] = "Company name is required";
                    return RedirectToAction("Settings");
                }
                    
                if (string.IsNullOrEmpty(model.Position))
                {
                    TempData["ErrorMessage"] = "Position is required";
                    return RedirectToAction("Settings");
                }
                    
                // Parse dates from d/m/y format to DateTime
                if (string.IsNullOrEmpty(StartDate))
                {
                    TempData["ErrorMessage"] = "Start date is required";
                    return RedirectToAction("Settings");
                }
                
                try
                {
                    // Parse start date (format: d/m/y)
                    string[] startParts = StartDate.Split('/');
                    if (startParts.Length == 3)
                    {
                        // Adjust for 2-digit year
                        string year = startParts[2];
                        if (year.Length == 2)
                        {
                            year = "20" + year; // Assuming 21st century
                        }
                        
                        model.StartDate = new DateTime(int.Parse(year), int.Parse(startParts[1]), int.Parse(startParts[0]));
                        System.Diagnostics.Debug.WriteLine($"Parsed start date: {model.StartDate}");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid start date format. Please use d/m/y format.";
                        return RedirectToAction("Settings");
                    }
                    
                    // Parse end date if provided and not current job
                    if (!model.IsCurrentJob && !string.IsNullOrEmpty(EndDate))
                    {
                        string[] endParts = EndDate.Split('/');
                        if (endParts.Length == 3)
                        {
                            // Adjust for 2-digit year
                            string year = endParts[2];
                            if (year.Length == 2)
                            {
                                year = "20" + year; // Assuming 21st century
                            }
                            
                            model.EndDate = new DateTime(int.Parse(year), int.Parse(endParts[1]), int.Parse(endParts[0]));
                            System.Diagnostics.Debug.WriteLine($"Parsed end date: {model.EndDate}");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid end date format. Please use d/m/y format.";
                            return RedirectToAction("Settings");
                        }
                    }
                    else if (!model.IsCurrentJob)
                    {
                        TempData["ErrorMessage"] = "End date is required when not current job";
                        return RedirectToAction("Settings");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Date parsing error: {ex.Message}");
                    TempData["ErrorMessage"] = "Error parsing dates. Please ensure they are in d/m/y format.";
                    return RedirectToAction("Settings");
                }
                
                // Handle null fields
                model.City = model.City ?? string.Empty;
                model.Description = model.Description ?? string.Empty;
                    
                // Assign to current user
                model.UserId = userId;
                model.CreatedAt = DateTime.UtcNow;
                
                System.Diagnostics.Debug.WriteLine($"Adding experience to database: User {userId}, Company {model.Company}, Position {model.Position}");
                
                _context.Experiences.Add(model);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Experience saved successfully with ID: {model.Id}");
                
                TempData["SuccessMessage"] = "Experience saved successfully!";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"SaveExperience error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Include stack trace for debugging
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"An error occurred while saving the experience: {ex.Message}";
                return RedirectToAction("Settings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExperiences()
        {
            try
            {
                // Get the current user's ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Fetch the user's experiences
                var experiences = await _context.Experiences
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();
                
                return JsonResponse(true, "Experiences retrieved successfully", experiences);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"GetExperiences error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return JsonResponse(false, "An error occurred while retrieving experiences");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            System.Diagnostics.Debug.WriteLine("ChangePassword method called");
            System.Diagnostics.Debug.WriteLine($"Received values - oldPassword length: {oldPassword?.Length ?? 0}, newPassword length: {newPassword?.Length ?? 0}, confirmPassword length: {confirmPassword?.Length ?? 0}");
            
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    System.Diagnostics.Debug.WriteLine("Password validation failed: One or more fields are empty");
                    TempData["ErrorMessage"] = "All password fields are required";
                    return RedirectToAction("Settings");
                }

                if (newPassword != confirmPassword)
                {
                    System.Diagnostics.Debug.WriteLine("Password validation failed: New password and confirmation don't match");
                    TempData["ErrorMessage"] = "New password and confirmation do not match";
                    return RedirectToAction("Settings");
                }

                // Minimum password requirements
                if (newPassword.Length < 6)
                {
                    System.Diagnostics.Debug.WriteLine("Password validation failed: Password too short");
                    TempData["ErrorMessage"] = "Password must be at least 6 characters long";
                    return RedirectToAction("Settings");
                }

                // Get current user
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                System.Diagnostics.Debug.WriteLine($"Finding user with ID: {userId}");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("User not found in database");
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction("Settings");
                }

                System.Diagnostics.Debug.WriteLine($"User found: {user.Id}, {user.Email}");
                System.Diagnostics.Debug.WriteLine($"Current password hash: {user.PasswordHash}");
                System.Diagnostics.Debug.WriteLine($"Old password provided: {oldPassword}");

                // Direct string comparison instead of BCrypt verification
                bool isOldPasswordValid = (user.PasswordHash == oldPassword);
                System.Diagnostics.Debug.WriteLine($"Old password verification result: {isOldPasswordValid}");
                
                if (!isOldPasswordValid)
                {
                    System.Diagnostics.Debug.WriteLine("Old password verification failed");
                    TempData["ErrorMessage"] = "Current password is incorrect";
                    return RedirectToAction("Settings");
                }

                // Update password directly without hashing
                try
                {
                    user.PasswordHash = newPassword;
                    System.Diagnostics.Debug.WriteLine("User password updated, saving changes...");
                    
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Changes saved successfully");
                    
                    TempData["SuccessMessage"] = "Password changed successfully";
                    return RedirectToAction("Settings");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating password: {ex.Message}");
                    throw; // Re-throw to be caught by outer catch block
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again later.";
                return RedirectToAction("Settings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveEducation(Education model, string StartDate, string EndDate)
        {
            try
            {
                // Debug incoming data
                System.Diagnostics.Debug.WriteLine($"SaveEducation: Received data - School: {model.School}, Degree: {model.Degree}, Field: {model.Field}, StartDate: {StartDate}, EndDate: {EndDate}");
                
                // Get the current user
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Validation
                if (string.IsNullOrEmpty(model.School))
                {
                    TempData["ErrorMessage"] = "School name is required";
                    return RedirectToAction("Settings");
                }
                    
                if (string.IsNullOrEmpty(model.Degree))
                {
                    TempData["ErrorMessage"] = "Degree is required";
                    return RedirectToAction("Settings");
                }
                    
                // Parse dates from d/m/y format to DateTime
                if (string.IsNullOrEmpty(StartDate))
                {
                    TempData["ErrorMessage"] = "Start date is required";
                    return RedirectToAction("Settings");
                }
                
                try
                {
                    // Parse start date (format: d/m/y)
                    string[] startParts = StartDate.Split('/');
                    if (startParts.Length == 3)
                    {
                        // Adjust for 2-digit year
                        string year = startParts[2];
                        if (year.Length == 2)
                        {
                            year = "20" + year; // Assuming 21st century
                        }
                        
                        model.StartDate = new DateTime(int.Parse(year), int.Parse(startParts[1]), int.Parse(startParts[0]));
                        System.Diagnostics.Debug.WriteLine($"Parsed start date: {model.StartDate}");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid start date format. Please use d/m/y format.";
                        return RedirectToAction("Settings");
                    }
                    
                    // Parse end date if provided
                    if (!string.IsNullOrEmpty(EndDate))
                    {
                        string[] endParts = EndDate.Split('/');
                        if (endParts.Length == 3)
                        {
                            // Adjust for 2-digit year
                            string year = endParts[2];
                            if (year.Length == 2)
                            {
                                year = "20" + year; // Assuming 21st century
                            }
                            
                            model.EndDate = new DateTime(int.Parse(year), int.Parse(endParts[1]), int.Parse(endParts[0]));
                            System.Diagnostics.Debug.WriteLine($"Parsed end date: {model.EndDate}");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid end date format. Please use d/m/y format.";
                            return RedirectToAction("Settings");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Date parsing error: {ex.Message}");
                    TempData["ErrorMessage"] = "Error parsing dates. Please ensure they are in d/m/y format.";
                    return RedirectToAction("Settings");
                }
                
                // Handle null fields
                model.Field = model.Field ?? string.Empty;
                model.Location = model.Location ?? string.Empty;
                    
                // Assign to current user
                model.UserId = userId;
                model.CreatedAt = DateTime.UtcNow;
                
                System.Diagnostics.Debug.WriteLine($"Adding education to database: User {userId}, School {model.School}, Degree {model.Degree}");
                
                _context.Educations.Add(model);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Education saved successfully with ID: {model.Id}");
                
                TempData["SuccessMessage"] = "Education saved successfully!";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"SaveEducation error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Include stack trace for debugging
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"An error occurred while saving education: {ex.Message}";
                return RedirectToAction("Settings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEducations()
        {
            try
            {
                // Get the current user's ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Fetch the user's educations
                var educations = await _context.Educations
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();
                
                return JsonResponse(true, "Educations retrieved successfully", educations);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"GetEducations error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return JsonResponse(false, "An error occurred while retrieving educations");
            }
        }
    }
}
