using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Entities
{
    public class TaxServerMapping
    {
        [Key]
        [Required]
        [StringLength(20)]
        public string TaxCode { get; set; }

        [Required]
        [StringLength(100)]
        public string ServerHost { get; set; }

        [Required]
        [StringLength(100)]
        public string Catalog { get; set; } = "BosEVATbizzi";

        [Required]
        [StringLength(50)]
        public string User { get; set; }

        [Required]
        [StringLength(200)]
        public string Password { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DecryptedPassword { get; set; }
    }
}
