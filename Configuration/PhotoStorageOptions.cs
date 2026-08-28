namespace CongoTravel.Configuration
{
    /// <summary>Options stockage médias CongoTravel (préfixe S3 / dossier local).</summary>
    public class PhotoStorageOptions
    {
        public const string SectionName = "AWS:S3";

        /// <summary>Préfixe racine des objets photo (sans slash final). Défaut : congotravel/photos</summary>
        public string PhotoKeyPrefix { get; set; } = "congotravel/photos";
    }
}
