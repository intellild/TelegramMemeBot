using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Telegram.Synthetic.Dawn.Models
{
    [Table("meme")]
    public class Meme
    {
        public enum MemeType
        {
            Message,
            Photo,
            ExternalPhoto
        }

        [Column("id")] public int Id { get; set; }
        [Column("type")] [Required] public MemeType Type { get; set; }
        [Column("content")] [Required] public string Content { get; set; }
        [Column("alias")] [Required] public string Alias { get; set; }
    }
}