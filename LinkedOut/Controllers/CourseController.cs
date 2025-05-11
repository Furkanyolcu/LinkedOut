using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LinkedOut.Models;
using System.Security.Claims;

namespace LinkedOut.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Reviews)
                .Where(c => c.IsPublished)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            ViewBag.IsEnrolled = course.Enrollments.Any(e => e.StudentId == userId);
            ViewBag.IsInstructor = course.InstructorId == userId;

            return View(course);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Course course)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                course.InstructorId = userId;
                course.CreatedAt = DateTime.UtcNow;

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = course.Id });
            }
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var existingEnrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);

            if (existingEnrollment != null)
            {
                return BadRequest("You are already enrolled in this course");
            }

            var enrollment = new CourseEnrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> Review(int courseId, int rating, string comment)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);

            if (enrollment == null)
            {
                return BadRequest("You must be enrolled in the course to leave a review");
            }

            var existingReview = await _context.CourseReviews
                .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = comment;
            }
            else
            {
                var review = new CourseReview
                {
                    CourseId = courseId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CourseReviews.Add(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

            if (course == null)
            {
                return NotFound();
            }

            course.IsPublished = true;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = course.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);

            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MyCourses()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var enrolledCourses = await _context.CourseEnrollments
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .Select(e => e.Course)
                .ToListAsync();

            var instructorCourses = await _context.Courses
                .Where(c => c.InstructorId == userId)
                .Include(c => c.Enrollments)
                .ToListAsync();

            ViewBag.EnrolledCourses = enrolledCourses;
            ViewBag.InstructorCourses = instructorCourses;

            return View();
        }
    }
} 