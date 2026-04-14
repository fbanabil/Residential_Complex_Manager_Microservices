namespace ResidentialAreas.API.EntityModels
{
    [Table("Images")]
    [Index(nameof(Code))]
    public class Image
    {
        [Key]
        [Required(ErrorMessage = "The image ID is required.")]
        public Guid? Id { get; set; }


        [Required(ErrorMessage ="The image type is required.")]
        [Column(TypeName = "varchar(20)")]
        public ImageType? ImageType { get; set; }


        [Required(ErrorMessage = "The image code is required.")]
        public long Code { get; set; }


        [Required(ErrorMessage = "The image URL is required.")]
        public string? Url { get; set; }
    }
}
