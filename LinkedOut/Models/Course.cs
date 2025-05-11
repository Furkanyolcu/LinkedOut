using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LinkedOut.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InstructorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string ThumbnailUrl { get; set; }
        public string VideoUrl { get; set; }
        public string? Category { get; set; }
        public int Duration { get; set; } // in minutes
        public int Level { get; set; } // 1: Beginner, 2: Intermediate, 3: Advanced
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; } = false;

        // Navigation properties
        public virtual User Instructor { get; set; }
        public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public virtual ICollection<CourseReview> Reviews { get; set; } = new List<CourseReview>();
    }

    public class CourseEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; } = false;
        public decimal? Rating { get; set; }

        // Navigation properties
        public virtual Course Course { get; set; }
        public virtual User Student { get; set; }
    }

    public class CourseReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Course Course { get; set; }
        public virtual User User { get; set; }
    }
} 