using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Common.Models
{
    public class Summary
    {
        [Key]
        public Guid GameId { get; set; }
        [Key]
        public Guid PlayerId { get; set; }
        public int Rank { get; set; }
        public int Score { get; set; }
    }
}
