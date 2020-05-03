using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Telegram.Synthetic.Dawn.Models
{
    [Table("user_alias")]
    public class UserAlias
    {
        [Column("id")] public int Id { get; set; }
        [Column("user_id")] [Required] public int UserId { get; set; }
        [Column("alias")] [Required] public string Alias { get; set; }
    }
}