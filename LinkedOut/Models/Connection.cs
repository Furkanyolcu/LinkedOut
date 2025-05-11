using System;
using System.ComponentModel.DataAnnotations;

namespace LinkedOut.Models
{
    public class Connection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public int AddresseeId { get; set; }

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User Requester { get; set; }
        public virtual User? Addressee { get; set; }
    }

    public enum ConnectionStatus
    {
        Pending,
        Accepted,
        Rejected,
        Blocked
    }
} 