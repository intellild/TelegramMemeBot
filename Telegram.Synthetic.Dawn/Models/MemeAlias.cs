using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Telegram.Synthetic.Dawn.Models
{
    [Table("meme_alias")]
    public class MemeAlias
    {
        [Column("id")] public int Id { get; set; }
        [Column("meme_id")] [Required] public int MemeId { get; set; }
        [Column("user_id")] [Required] public int UserId { get; set; }
    }
}