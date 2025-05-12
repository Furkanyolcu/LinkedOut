using System;
using System.ComponentModel.DataAnnotations;

namespace LinkedOut.Models
{
    public class Education
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string School { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Degree { get; set; } = string.Empty;

        [StringLength(100)]
        public string Field { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
} 