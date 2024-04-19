using System.ComponentModel.DataAnnotations;

namespace BlazingBlog.Data.Models
{
    public class Subscriber
    {
        public int Id { get; set; }

        [EmailAddress, Required, MaxLength(150)]
        public string Email { get; set; } = default!;

        [Required, MaxLength(25)]
        public string Name { get; set; } = default!;
        public DateTime SubscribedOn { get; set; }
    }
}
