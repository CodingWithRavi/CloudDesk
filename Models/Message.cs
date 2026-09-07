using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudDesk.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        // Sirf receiver ka read status.
        // Viewer (UserC) isko kabhi change nahi karega.
        public bool IsReadByReceiver { get; set; }

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser? Sender { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser? Receiver { get; set; }
    }
}