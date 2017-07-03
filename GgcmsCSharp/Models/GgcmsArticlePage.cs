namespace GgcmsCSharp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class GgcmsArticlePage
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        
        public string Content { get; set; }

        
        [StringLength(255)]
        public string Title { get; set; }

        public int Article_Id { get; set; }

    }
}
