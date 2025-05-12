using System;
using System.ComponentModel.DataAnnotations;

namespace LinkedOut.Models
{
    public class Experience
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Company { get; set; }

        [Required]
        [StringLength(100)]
        public string Position { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        public bool IsCurrentJob { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
} 